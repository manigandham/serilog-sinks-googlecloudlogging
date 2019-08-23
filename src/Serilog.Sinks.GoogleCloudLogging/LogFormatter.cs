using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.GoogleCloudLogging
{
    internal class LogFormatter
    {
        private readonly GoogleCloudLoggingSinkOptions _sinkOptions;
        private readonly MessageTemplateTextFormatter _messageTemplateTextFormatter;

        public LogFormatter(GoogleCloudLoggingSinkOptions sinkOptions, MessageTemplateTextFormatter messageTemplateTextFormatter)
        {
            _sinkOptions = sinkOptions;
            _messageTemplateTextFormatter = messageTemplateTextFormatter;
        }

        public string RenderEventMessage(LogEvent e, StringWriter writer)
        {
            // output template takes priority for formatting event
            if (_messageTemplateTextFormatter != null)
            {
                writer.GetStringBuilder().Clear();
                _messageTemplateTextFormatter.Format(e, writer);
                return writer.ToString();
            }

            // otherwise manually format message and handle exceptions
            var lines = new List<string>();
            var msg = e.RenderMessage();
            if (!String.IsNullOrWhiteSpace(msg))
                lines.Add(msg);

            if (e.Exception != null)
            {
                //if (e.Exception.GetType() == typeof(AggregateException))
                //{
                //    // ErrorReporting won't report all InnerExceptions for an AggregateException. This work-around isn't perfect but better than the default behavior
                //    lines.AddRange(((AggregateException) e.Exception).Flatten().InnerExceptions.Select(s => s.Message));
                //}

                lines.Add(e.Exception.ToString());
            }

            return String.Join("\n", lines);
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

                case ScalarValue scalarValue
                    when scalarValue.Value is short || scalarValue.Value is ushort || scalarValue.Value is int
                         || scalarValue.Value is uint || scalarValue.Value is long || scalarValue.Value is ulong
                         || scalarValue.Value is float || scalarValue.Value is double || scalarValue.Value is decimal:

                    // all numbers are converted to double and may lose precision
                    // numbers should be sent as strings if they do not fit in a double
                    jsonStruct.Fields.Add(propKey, Value.ForNumber(Convert.ToDouble(scalarValue.Value)));
                    break;

                case ScalarValue scalarValue when scalarValue.Value is string stringValue:
                    jsonStruct.Fields.Add(propKey, Value.ForString(stringValue));
                    CheckIfSourceContext(log, propKey, stringValue);
                    break;

                case ScalarValue scalarValue:
                    // handle all other scalar values as strings
                    var strValue = scalarValue.Value.ToString();
                    jsonStruct.Fields.Add(propKey, Value.ForString(strValue));
                    CheckIfSourceContext(log, propKey, strValue);
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
                        WritePropertyAsJson(log, dictionaryChild, childProperty.Key.Value?.ToString(), childProperty.Value);

                    jsonStruct.Fields.Add(propKey, Value.ForStruct(dictionaryChild));
                    break;
            }
        }

        /// <summary>
        /// Writes event properties as labels for a GCP log entry.
        /// GCP log labels are a flat key/value namespace so all child event properties will be prefixed with parent property names "parentkey.childkey" similar to json path.
        /// </summary>
        public void WritePropertyAsLabel(LogEntry log, string propertyKey, LogEventPropertyValue propertyValue)
        {
            switch (propertyValue)
            {
                case ScalarValue scalarValue when scalarValue.Value is null:
                    log.Labels.Add(propertyKey, String.Empty);
                    break;

                case ScalarValue scalarValue:
                    var stringValue = scalarValue.Value.ToString();
                    log.Labels.Add(propertyKey, stringValue);
                    CheckIfSourceContext(log, propertyKey, stringValue);
                    break;

                case SequenceValue sequenceValue:
                    log.Labels.Add(propertyKey, String.Join(",", sequenceValue.Elements));
                    break;

                case StructureValue structureValue when structureValue.Properties.Count > 0:
                    foreach (var childProperty in structureValue.Properties)
                        WritePropertyAsLabel(log, propertyKey + "." + childProperty.Name, childProperty.Value);

                    break;

                case DictionaryValue dictionaryValue when dictionaryValue.Elements.Count > 0:
                    foreach (var childProperty in dictionaryValue.Elements)
                        WritePropertyAsLabel(log, propertyKey + "." + childProperty.Key.ToString().Replace("\"", ""), childProperty.Value);

                    break;
            }
        }

        private void CheckIfSourceContext(LogEntry log, string propertyKey, string stringValue)
        {
            if (_sinkOptions.UseSourceContextAsLogName && propertyKey.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                log.LogName = new LogName(_sinkOptions.ProjectId, stringValue).ToString();
        }
    }
}
