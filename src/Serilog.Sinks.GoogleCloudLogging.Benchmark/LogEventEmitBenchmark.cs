using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace Serilog.Sinks.GoogleCloudLogging.Benchmark;

[MemoryDiagnoser]
[SimpleJob(runtimeMoniker: RuntimeMoniker.Net10_0, baseline: false)]
[SimpleJob(runtimeMoniker: RuntimeMoniker.Net90, baseline: false)]
[SimpleJob(runtimeMoniker: RuntimeMoniker.Net80, baseline: true)]
public class LogEventEmitBenchmark
{
    private readonly GoogleCloudLoggingSink _sink = new("project_test", new JsonFormatter());

    [Benchmark]
    [ArgumentsSource(nameof(Data))]
    public void CreateEventsBatch(IReadOnlyCollection<LogEvent> events)
    {
        _sink.CreateEventsBatch(events);
    }

    public IEnumerable<IReadOnlyCollection<LogEvent>> Data()
    {
        var timeStamp = DateTimeOffset.UtcNow;
        var mtParser = new MessageTemplateParser();
        var mt = mtParser.Parse("Hello {@World}");
        return
        [
            [
                new LogEvent(timestamp: timeStamp,
                    level: LogEventLevel.Information,
                    exception: null,
                    messageTemplate: mt,
                    properties: [new LogEventProperty("World", new ScalarValue("Hello World!"))])
            ],
            Enumerable.Range(1, 10)
                .Select(t => new LogEvent(
                    timestamp: timeStamp.AddMilliseconds(t),
                    level: LogEventLevel.Information,
                    exception: null,
                    messageTemplate: mt,
                    properties: [new LogEventProperty("World", new ScalarValue("Hello World!"))]))
                .ToArray(),
            Enumerable.Range(1, 100)
                .Select(t => new LogEvent(
                    timestamp: timeStamp.AddMilliseconds(t),
                    level: LogEventLevel.Information,
                    exception: null,
                    messageTemplate: mt,
                    properties: [new LogEventProperty("World", new ScalarValue("Hello World!"))]))
                .ToArray()
        ];
    }
}
