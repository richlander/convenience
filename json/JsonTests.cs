
using BenchmarkDotNet.Attributes;
using ReleaseJson;

namespace Tests;

#pragma warning disable CA1822

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class JsonTests
{
    [Benchmark]
    public Task<string> FromJson() => GetFromJson.Go();


    [Benchmark]
    public Task<string> WithSourceGeneration() => JsonWithSourceGeneration.Go();

    // [Benchmark]
    // public Task<byte[]> WithUtf8() => JsonWithUtf8.Go();
}
