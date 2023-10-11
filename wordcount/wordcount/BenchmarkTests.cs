using BenchmarkDotNet.Attributes;

namespace BenchmarkTests;

[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[MemoryDiagnoser]
public class BenchmarkTests
{
    public string FilePath { get; set; } = BenchmarkData.BenchmarkValues.ShortFilePath;

    [Benchmark(Baseline = true)]
    public void FileOpenHandle() => FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenHandleNoSearchValues() => FileOpenHandleNoSearchValuesBenchmark.FileOpenHandleNoSearchValuesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpen() => FileOpenBenchmark.FileOpenBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextCharSearchValues() => FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenText() => FileOpenTextBenchmark.FileOpenTextBenchmark.Count(FilePath);

    [Benchmark]
    public void FileReadLines() => FileReadLinesBenchmark.FileReadLinesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileReadAllLines() => FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count(FilePath);
}
