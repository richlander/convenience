using BenchmarkDotNet.Attributes;

namespace BenchmarkTests;

[HideColumns("Error", "StdDev", "Median", "RatioSD")]
// [MemoryDiagnoser]
public class BenchmarkTests
{
    [Params("Clarissa_Harlowe/summary.md", "Clarissa_Harlowe/clarissa_volume1.txt")]
    public string FilePath { get; set; } = "";

    [Benchmark(Baseline = true)]
    public void FileOpenHandle() => FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenHandleAsciiOnly() => FileOpenHandleAsciiOnlyBenchmark.FileOpenHandleAsciiOnlyBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpen() => FileOpenBenchmark.FileOpenBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextCharSearchValues() => FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextChar() => FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextReadLine() => FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count(FilePath);
    
    [Benchmark]
    public void FileReadLines() => FileReadLinesBenchmark.FileReadLinesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileReadAllLines() => FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count(FilePath);
}
