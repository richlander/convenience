
using BenchmarkDotNet.Attributes;

namespace Tests;

#pragma warning disable CA1822

[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[MemoryDiagnoser]
public class JsonTests
{
    // [Benchmark]
    // public Task<string> DownloadFileBenchmarks() => DownloadFileBenchmark.DownloadFileBenchmark.Run();

    [Benchmark]
    public Task<string> WithJsonSerializer() => JsonSerializerBenchmark.JsonSerializerBenchmark.Run();

    [Benchmark]
    public Task<string> WithJsonSerializerSourceGenerator() => JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run();

    [Benchmark]
    public Task<string> WithJsonDocument() => JsonDocumentBenchmark.JsonDocumentBenchmark.Run();

    [Benchmark]
    public Task<string> WithUtf8JsonReaderWriter() => Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run();
}
