using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.GoogleCloudLogging
{
    internal class LogFormatter
    {
        private readonly string _projectId;
        private readonly bool _useSourceContextAsLogName;
        private readonly bool _useLogCorrelation;
        private readonly MessageTemplateTextFormatter? _messageTemplateTextFormatter;

        private static readonly Dictionary<string, string> LogNameCache = new(StringComparer.Ordinal);
        private static readonly Regex LogNameUnsafeChars = new("[^0-9A-Z._/-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        public LogFormatter(
            string projectId,
            bool useSourceContextAsLogName,
            bool useLogCorrelation,
            MessageTemplateTextFormatter? messageTemplateTextFormatter)
        {
            _projectId = projectId;
            _useSourceContextAsLogName = useSourceContextAsLogName;
            _useLogCorrelation = useLogCorrelation;
            _messageTemplateTextFormatter = messageTemplateTextFormatter;
        }

        public string RenderEventMessage(LogEvent e, StringWriter writer)
        {
            writer.GetStringBuilder().Clear();

            // output template takes priority for formatting event
            if (_messageTemplateTextFormatter != null)
            {
                _messageTemplateTextFormatter.Format(e, writer);
            }
            else
            {
                // otherwise manually format message and handle exceptions
                e.RenderMessage(writer);

                if (e.Exception != null)
                {
                    // check the current message length and add new line before exception stack trace if needed
                    if (writer.GetStringBuilder().Length > 0)
                        writer.WriteLine();

                    writer.Write(e.Exception.ToString());
                }
            }

            return writer.ToString();
        }

        /// <summary>
        /// Writes event properties as a JSON object for a GCP log entry.
        /// </summary>
        public void WritePropertyAsJson(LogEntry log, Struct jsonStruct, string propKey, LogEventPropertyValue propValue)
        {
            switch (propValue)
            {
                case ScalarValue scalarValue when scalarValue.Value is null:
                    jsonStruct.Fields.Add(propKey, Value.ForNull());
                    break;

                case ScalarValue scalarValue when scalarValue.Value is bool boolValue:
                    jsonStruct.Fields.Add(propKey, Value.ForBool(boolValue));
                    break;

                case ScalarValue scalarValue when scalarValue.Value is short or ushort or int or uint or long or ulong or float or double or decimal:
                    // all numbers are converted to double and may lose precision
                    // numbers should be sent as strings if they do not fit in a double
                    jsonStruct.Fields.Add(propKey, Value.ForNumber(Convert.ToDouble(scalarValue.Value)));
                    break;

                case ScalarValue scalarValue when scalarValue.Value is string stringValue:
                    jsonStruct.Fields.Add(propKey, Value.ForString(stringValue));
                    CheckForSpecialProperties(log, propKey, stringValue);
                    break;

                case ScalarValue scalarValue:
                    // handle all other scalar values as strings
                    var strValue = scalarValue.Value.ToString() ?? String.Empty;
                    jsonStruct.Fields.Add(propKey, Value.ForString(strValue));
                    CheckForSpecialProperties(log, propKey, strValue);
                    break;

                case SequenceValue sequenceValue:
                    var sequenceChild = new Struct();
                    for (var i = 0; i < sequenceValue.Elements.Count; i++)
                        WritePropertyAsJson(log, sequenceChild, i.ToString(), sequenceValue.Elements[i]);

                    jsonStruct.Fields.Add(propKey, Value.ForList(sequenceChild.Fields.Values.ToArray()));
                    break;

                case StructureValue structureValue:
                    var structureChild = new Struct();
                    foreach (var childProperty in structureValue.Properties)
                        WritePropertyAsJson(log, structureChild, childProperty.Name, childProperty.Value);

                    jsonStruct.Fields.Add(propKey, Value.ForStruct(structureChild));
                    break;

                case DictionaryValue dictionaryValue:
                    var dictionaryChild = new Struct();
                    foreach (var childProperty in dictionaryValue.Elements)
                        WritePropertyAsJson(log, dictionaryChild, childProperty.Key.Value?.ToString() ?? String.Empty, childProperty.Value);

                    jsonStruct.Fields.Add(propKey, Value.ForStruct(dictionaryChild));
                    break;
            }
        }

        /// <summary>
        /// Writes event properties as labels for a GCP log entry.
        /// GCP log labels are a flat key/value namespace so all child event properties will be prefixed with parent property names "parentkey.childkey" similar to json path.
        /// </summary>
        public void WritePropertyAsLabel(LogEntry log, string propKey, LogEventPropertyValue propValue)
        {
            switch (propValue)
            {
                case ScalarValue scalarValue:
                    var stringValue = scalarValue.Value?.ToString() ?? String.Empty;
                    log.Labels.Add(propKey, stringValue);
                    CheckForSpecialProperties(log, propKey, stringValue);
                    break;

                case SequenceValue sequenceValue:
                    log.Labels.Add(propKey, String.Join(",", sequenceValue.Elements));
                    break;

                case StructureValue structureValue when structureValue.Properties.Count > 0:
                    foreach (var childProperty in structureValue.Properties)
                        WritePropertyAsLabel(log, propKey + "." + childProperty.Name, childProperty.Value);

                    break;

                case DictionaryValue dictionaryValue when dictionaryValue.Elements.Count > 0:
                    foreach (var childProperty in dictionaryValue.Elements)
                        WritePropertyAsLabel(log, propKey + "." + childProperty.Key.ToString().Replace("\"", ""), childProperty.Value);

                    break;
            }
        }

        private void CheckForSpecialProperties(LogEntry log, string key, string value)
        {
            if (_useSourceContextAsLogName && key.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                log.LogName = CreateLogName(_projectId, value);

            if (_useLogCorrelation)
            {
                if (key.Equals("TraceId", StringComparison.OrdinalIgnoreCase))
                    log.Trace = $"projects/{_projectId}/traces/{value}";

                if (key.Equals("SpanId", StringComparison.OrdinalIgnoreCase))
                    log.SpanId = value;
            }
        }

        public static string CreateLogName(string projectId, string name)
        {
            // cache log name to avoid formatting name for every statement
            // TODO: potential memory leak because cached names are never cleared, however shouldnt be an issue even with thousands of entries
            if (!LogNameCache.TryGetValue(name, out var logName))
            {
                // name must only contain letters, numbers, underscore, hyphen, forward slash and period
                // limited to 512 characters and must be url-encoded
                var safeChars = LogNameUnsafeChars.Replace(name, String.Empty);
                var clean = UrlEncoder.Default.Encode(safeChars);

                // LogName class creates templated string matching GCP requirements
                logName = new LogName(projectId, clean).ToString();

                LogNameCache.Add(name, logName);
            }

            return logName;
        }
    }
}
