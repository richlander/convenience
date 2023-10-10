using BenchmarkDotNet.Attributes;

namespace BenchmarkTests;

[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[MemoryDiagnoser]
public class BenchmarkTests
{

    public string FilePath { get; set; } = BenchmarkData.BenchmarkValues.FilePath;

    [Benchmark(Baseline = true)]
    public void FileOpenHandle() => FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpen() => FileOpenBenchmark.FileOpenBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenSearchValues() => FileOpenHandleSearchValuesBenchmark.FileOpenHandleSearchValuesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenText() => FileOpenTextBenchmark.FileOpenTextBenchmark.Count(FilePath);

    [Benchmark]
    public void FileReadLines() => FileReadLinesBenchmark.FileReadLinesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileReadAllLines() => FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count(FilePath);
}