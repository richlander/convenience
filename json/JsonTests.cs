
using BenchmarkDotNet.Attributes;

namespace Tests;

#pragma warning disable CA1822

[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[MemoryDiagnoser]
public class JsonTests
{
    // private const int EXPECTEDLENGTH = 465;
    // [Benchmark]
    // public Task<string> DownloadFileBenchmarks() => DownloadFileBenchmark.DownloadFileBenchmark.Run();
    private Stream? _stream = null;

    [GlobalSetup]
    public async Task Setup()
    {
        HttpClient httpClient= new();
        var releaseMessage = await httpClient.GetAsync(FakeTestData.URL, HttpCompletionOption.ResponseContentRead);
        _stream = await releaseMessage.Content.ReadAsStreamAsync();
    }

    [Benchmark]
    public void WithJsonSerializer() => JsonSerializerBenchmark.JsonSerializerBenchmark.Run(_stream!);

    [Benchmark]
    public void WithJsonSerializerSourceGenerator() => JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run(_stream!);

    [Benchmark]
    public void WithJsonDocument() => JsonDocumentBenchmark.JsonDocumentBenchmark.Run(_stream!);

    // [Benchmark]
    // public async Task WithUtf8JsonReaderWriter() => await Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run(_stream!);
}
