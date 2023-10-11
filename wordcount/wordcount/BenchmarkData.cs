using System.Net.NetworkInformation;

namespace BenchmarkData;

public static class BenchmarkValues
{

    public static string FilePath => Path.Combine(DirectoryPath, "clarissa_volume1.txt");

    public static string ShortFilePath => Path.Combine(DirectoryPath, "summary.md");

    public static string DirectoryPath => Path.Combine(Path.GetDirectoryName(typeof(BenchmarkValues).Assembly.Location)!, "Clarissa_Harlowe");

    public static int Size => 16 * 1024;

    public static List<Benchmark> Benchmarks => 
        [
            new(nameof(FileOpenHandleBenchmark), FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count),
            new(nameof(FileOpenHandleSearchValuesBenchmark), FileOpenHandleSearchValuesBenchmark.FileOpenHandleSearchValuesBenchmark.Count),
            new(nameof(FileOpenBenchmark), FileOpenBenchmark.FileOpenBenchmark.Count),
            new(nameof(FileOpenTextSpanCharBenchmark), FileOpenTextSpanCharBenchmark.FileOpenTextSpanCharBenchmark.Count),
            new(nameof(FileOpenTextReadLineBenchmark), FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count),
            new(nameof(FileReadLinesBenchmark), FileOpenBenchmark.FileOpenBenchmark.Count),
            new(nameof(FileReadAllLinesBenchmark), FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count),
        ];

    public static Benchmark Benchmark { get; set; } = Benchmarks[0];
}

public record Benchmark(string Name, Func<string, Count> Test);

public record struct BenchmarkResult(int Pass, string Benchmark, TimeSpan Duration, Count Counts);

public record struct Count(int Lines, int Words, int Bytes, string File);
