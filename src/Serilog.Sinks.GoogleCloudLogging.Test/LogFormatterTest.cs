using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.GoogleCloudLogging.Test;

public class LogFormatterTest
{
    private readonly LogFormatter _formatter = new(null);

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(1000)]
    public void WillPreserveSequenceElementOrder(int count)
    {
        var sequence = new SequenceValue(Enumerable.Range(0, count).Select(i => new ScalarValue(i)));
        var result = new Struct();

        _formatter.WritePropertyAsJson(result, "items", sequence);

        var expected = Value.ForList(Enumerable.Range(0, count).Select(i => Value.ForNumber(i)).ToArray());
        Assert.Equal(expected, result.Fields["items"]);
    }

    [Fact]
    public void WillPreserveScalarTypesInSequences()
    {
        var id = Guid.Parse("ad9d33bf-94c6-4217-83ca-335096046e2b");
        var sequence = new SequenceValue(new[]
        {
            new ScalarValue(null),
            new ScalarValue(true),
            new ScalarValue(false),
            new ScalarValue(42),
            new ScalarValue(2.5m),
            new ScalarValue("text"),
            new ScalarValue(id)
        });
        var result = new Struct();

        _formatter.WritePropertyAsJson(result, "items", sequence);

        Assert.Equal(Value.ForList(
            Value.ForNull(),
            Value.ForBool(true),
            Value.ForBool(false),
            Value.ForNumber(42),
            Value.ForNumber(2.5),
            Value.ForString("text"),
            Value.ForString(id.ToString())), result.Fields["items"]);
    }

    [Fact]
    public void WillConvertNestedSequencesStructuresAndDictionaries()
    {
        var sequence = new SequenceValue(new LogEventPropertyValue[]
        {
            new SequenceValue(Array.Empty<LogEventPropertyValue>()),
            new SequenceValue(new[] { new ScalarValue(1), new ScalarValue(2) }),
            new StructureValue(new[]
            {
                new LogEventProperty("items", new SequenceValue(new[] { new ScalarValue(true), new ScalarValue(null) }))
            }),
            new DictionaryValue(new[]
            {
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    new ScalarValue(7), new SequenceValue(new[] { new ScalarValue("nested") }))
            })
        });
        var result = new Struct();

        _formatter.WritePropertyAsJson(result, "items", sequence);

        Assert.Equal(Value.ForList(
            Value.ForList(),
            Value.ForList(Value.ForNumber(1), Value.ForNumber(2)),
            Value.ForStruct(new Struct
            {
                Fields = { ["items"] = Value.ForList(Value.ForBool(true), Value.ForNull()) }
            }),
            Value.ForStruct(new Struct
            {
                Fields = { ["7"] = Value.ForList(Value.ForString("nested")) }
            })), result.Fields["items"]);
    }

    [Fact]
    public void WillContinueOmittingUnsupportedPropertyValues()
    {
        var unsupported = new UnsupportedPropertyValue();
        var result = new Struct();

        _formatter.WritePropertyAsJson(result, "unsupported", unsupported);
        _formatter.WritePropertyAsJson(result, "items", new SequenceValue(new LogEventPropertyValue[]
        {
            new ScalarValue(1), unsupported, new ScalarValue(null), new ScalarValue(2)
        }));

        Assert.False(result.Fields.ContainsKey("unsupported"));
        Assert.Equal(Value.ForList(Value.ForNumber(1), Value.ForNull(), Value.ForNumber(2)), result.Fields["items"]);
    }

    [Fact]
    public void WillAvoidIntermediateSequenceMapAllocations()
    {
        var sequence = new SequenceValue(Enumerable.Range(0, 100).Select(i => new ScalarValue(i)));
        for (var i = 0; i < 20; i++)
            _formatter.WritePropertyAsJson(new Struct(), "items", sequence);

        const int iterations = 100;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < iterations; i++)
            _formatter.WritePropertyAsJson(new Struct(), "items", sequence);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.InRange(allocatedBytes, 0, 12_000L * iterations);
    }

    private sealed class UnsupportedPropertyValue : LogEventPropertyValue
    {
        public override void Render(TextWriter output, string format = null, IFormatProvider formatProvider = null)
        {
            output.Write("unsupported");
        }
    }
}
