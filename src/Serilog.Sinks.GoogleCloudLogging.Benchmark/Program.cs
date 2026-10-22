// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Serilog.Sinks.GoogleCloudLogging.Benchmark;

_ = BenchmarkRunner.Run<LogEventEmitBenchmark>();