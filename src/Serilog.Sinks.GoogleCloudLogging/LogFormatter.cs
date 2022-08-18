using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.GoogleCloudLogging;

internal class LogFormatter
{
    private readonly ITextFormatter? _textFormatter;

    private static readonly Dictionary<string, string> LogNameCache = new(StringComparer.Ordinal);
    private static readonly Regex LogNameUnsafeChars = new("[^0-9A-Z._/-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    public LogFormatter(ITextFormatter? textFormatter)
    {
        _textFormatter = textFormatter;
    }

    public string RenderEventMessage(LogEvent e, StringWriter writer)
    {
        writer.GetStringBuilder().Clear();

        // use formatter if provided
        if (_textFormatter != null)
        {
            _textFormatter.Format(e, writer);
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
    public void WritePropertyAsJson(Struct jsonStruct, string propKey, LogEventPropertyValue propValue)
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
                break;

            case ScalarValue scalarValue:
                // handle all other scalar values as strings
                jsonStruct.Fields.Add(propKey, Value.ForString(scalarValue.Value?.ToString() ?? ""));
                break;

            case SequenceValue sequenceValue:
                var sequenceChild = new Struct();
                for (var i = 0; i < sequenceValue.Elements.Count; i++)
                    WritePropertyAsJson(sequenceChild, i.ToString(), sequenceValue.Elements[i]);

                jsonStruct.Fields.Add(propKey, Value.ForList(sequenceChild.Fields.Values.ToArray()));
                break;

            case StructureValue structureValue:
                var structureChild = new Struct();
                foreach (var childProperty in structureValue.Properties)
                    WritePropertyAsJson(structureChild, childProperty.Name, childProperty.Value);

                jsonStruct.Fields.Add(propKey, Value.ForStruct(structureChild));
                break;

            case DictionaryValue dictionaryValue:
                var dictionaryChild = new Struct();
                foreach (var childProperty in dictionaryValue.Elements)
                    WritePropertyAsJson(dictionaryChild, childProperty.Key.Value?.ToString() ?? "", childProperty.Value);

                jsonStruct.Fields.Add(propKey, Value.ForStruct(dictionaryChild));
                break;
        }
    }

    public static string CreateLogName(string projectId, string name)
    {
        // cache log name to avoid formatting name for every statement
        // TODO: potential memory leak because cached names are never cleared, however it shouldn't be an issue even with thousands of entries
        if (!LogNameCache.TryGetValue(name, out var logName))
        {
            // name must only contain: letters, numbers, underscore, hyphen, forward slash, period
            // limited to 512 characters and must be url-encoded (using 500 char limit here to be safe)
            var safeChars = LogNameUnsafeChars.Replace(name, "");
            var truncated = safeChars.Length > 500 ? safeChars.Substring(0, 500) : safeChars;
            var encoded = UrlEncoder.Default.Encode(safeChars);

            // LogName class creates templated string matching GCP requirements
            logName = new LogName(projectId, encoded).ToString();

            LogNameCache.Add(name, logName);
        }

        return logName;
    }
}