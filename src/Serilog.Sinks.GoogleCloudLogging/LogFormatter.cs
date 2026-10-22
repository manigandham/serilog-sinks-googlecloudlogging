using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.GoogleCloudLogging;

internal partial class LogFormatter
{
    private readonly ITextFormatter? _textFormatter;

    private static readonly Dictionary<string, string> LogNameCache = new(StringComparer.Ordinal);

    #if NET8_0_OR_GREATER
    [GeneratedRegex("[^0-9A-Z._/-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex LogNameUnsafeChars();
    #else 
      private static readonly Regex LogNameUnsafeCharsRegex = new("[^0-9A-Z._/-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
      private static  Regex LogNameUnsafeChars() => LogNameUnsafeCharsRegex;
    #endif

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

                //Exception message stacktrace goes as a separate exception property
                writer.Write(e.Exception.Message);
            }
        }

        return writer.ToString();
    }

    /// <summary>
    /// Writes event properties as a JSON object for a GCP log entry.
    /// </summary>
    public void WritePropertyAsJson(Struct jsonStruct, string propKey, LogEventPropertyValue propValue)
    {
        var value = CreatePropertyValue(propValue);
        if (value != null)
            jsonStruct.Fields.Add(propKey, value);
    }

    private Value? CreatePropertyValue(LogEventPropertyValue propValue)
    {
        switch (propValue)
        {
            case ScalarValue scalarValue when scalarValue.Value is null:
                return Value.ForNull();

            case ScalarValue scalarValue when scalarValue.Value is bool boolValue:
                return Value.ForBool(boolValue);

            case ScalarValue scalarValue when scalarValue.Value is short or ushort or int or uint or long or ulong or float or double or decimal:
                // all numbers are converted to double and may lose precision
                // numbers should be sent as strings if they do not fit in a double
                return Value.ForNumber(Convert.ToDouble(scalarValue.Value));

            case ScalarValue scalarValue when scalarValue.Value is string stringValue:
                return Value.ForString(stringValue);

            case ScalarValue scalarValue:
                // handle all other scalar values as strings
                return Value.ForString(scalarValue.Value?.ToString() ?? "");

            case SequenceValue sequenceValue:
                var sequenceChild = new ListValue();
                sequenceChild.Values.Capacity = sequenceValue.Elements.Count;
                for (var i = 0; i < sequenceValue.Elements.Count; i++)
                {
                    var element = CreatePropertyValue(sequenceValue.Elements[i]);
                    if (element != null)
                        sequenceChild.Values.Add(element);
                }

                return new Value { ListValue = sequenceChild };

            case StructureValue structureValue:
                var structureChild = new Struct();
                foreach (var childProperty in structureValue.Properties)
                    WritePropertyAsJson(structureChild, childProperty.Name, childProperty.Value);

                return Value.ForStruct(structureChild);

            case DictionaryValue dictionaryValue:
                var dictionaryChild = new Struct();
                foreach (var childProperty in dictionaryValue.Elements)
                    WritePropertyAsJson(dictionaryChild, childProperty.Key.Value?.ToString() ?? "", childProperty.Value);

                return Value.ForStruct(dictionaryChild);
        }

        return null;
    }

    public static string CreateLogName(string projectId, string name)
    {
        // cache log name to avoid formatting name for every statement
        // TODO: potential memory leak because cached names are never cleared, however it shouldn't be an issue even with thousands of entries
        if (!LogNameCache.TryGetValue(name, out var logName))
        {
            // name must only contain: letters, numbers, underscore, hyphen, forward slash, period
            // limited to 512 characters and must be url-encoded (using 500 char limit here to be safe)
            var safeChars = LogNameUnsafeChars().Replace(name, "");
            var truncated = safeChars.Length > 500 ? safeChars.Substring(0, 500) : safeChars;
            var encoded = UrlEncoder.Default.Encode(truncated);

            // LogName class creates templated string matching GCP requirements
            logName = new LogName(projectId, encoded).ToString();

            LogNameCache.Add(name, logName);
        }

        return logName;
    }
}