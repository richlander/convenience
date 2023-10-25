using BenchmarkDotNet.Attributes;

namespace BenchmarkTests;

[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[MemoryDiagnoser]
public class BenchmarkTests
{
    //[Params("Clarissa_Harlowe/summary.md","Clarissa_Harlowe/clarissa_volume1.txt")]
    [Params("Clarissa_Harlowe/clarissa_volume1.txt")]
    public string FilePath { get; set; } = "";

    [Benchmark(Baseline = true)]
    public void FileOpenHandleRune() => FileOpenHandleRuneBenchmark.FileOpenHandleRuneBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenHandleAsciiCheat() => FileOpenHandleAsciiCheatBenchmark.FileOpenHandleAsciiCheatBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenRune() => FileOpenRuneBenchmark.FileOpenRuneBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextCharSearchValues() => FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextChar() => FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count(FilePath);

    [Benchmark]
    public void FileOpenTextReadLine() => FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count(FilePath);
    
    [Benchmark]
    public void FileOpenTextReadLineSearchValues() => FileOpenTextReadLineSearchValuesBenchmark.FileOpenTextReadLineSearchValuesBenchmark.Count(FilePath);
    
    [Benchmark]
    public void FileReadLines() => FileReadLinesBenchmark.FileReadLinesBenchmark.Count(FilePath);

    [Benchmark]
    public void FileReadAllLines() => FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count(FilePath);
}
