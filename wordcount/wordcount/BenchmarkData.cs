using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BenchmarkData;

public static class BenchmarkValues
{

    public static string FilePath => Path.Combine(DirectoryPath, "clarissa_volume1.txt");

    public static string ShortFilePath => Path.Combine(DirectoryPath, "summary.md");

    public static string DirectoryPath => Path.Combine(Path.GetDirectoryName(typeof(BenchmarkValues).Assembly.Location)!, "Clarissa_Harlowe");

    public static int Size => 16 * 1024;

    public static List<Benchmark> Benchmarks => 
        [
            new(nameof(FileOpenHandleRuneBenchmark), FileOpenHandleRuneBenchmark.FileOpenHandleRuneBenchmark.Count),
            new(nameof(FileOpenHandleAsciiCheatBenchmark), FileOpenHandleAsciiCheatBenchmark.FileOpenHandleAsciiCheatBenchmark.Count),
            new(nameof(FileOpenRuneBenchmark), FileOpenRuneBenchmark.FileOpenRuneBenchmark.Count),
            new(nameof(FileOpenTextCharBenchmark), FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count),
            new(nameof(FileOpenTextCharSearchValuesBenchmark), FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenTextReadLineBenchmark), FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count),
            new(nameof(FileOpenTextReadLineSearchValuesBenchmark), FileOpenTextReadLineSearchValuesBenchmark.FileOpenTextReadLineSearchValuesBenchmark.Count),
            new(nameof(FileReadLinesBenchmark), FileOpenRuneBenchmark.FileOpenRuneBenchmark.Count),
            new(nameof(FileReadAllLinesBenchmark), FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count),
        ];

    public static Benchmark Benchmark { get; set; } = Benchmarks[0];

    public static SearchValues<byte> WhitespaceSearchAscii = SearchValues.Create((ReadOnlySpan<byte>)[9, 10, 11, 12, 13, 32, 194, 225, 226, 227]);

    public static SearchValues<char> WhitespaceSearch = SearchValues.Create(GetWhiteSpaceChars().ToArray());

    public static IEnumerable<char> GetWhiteSpaceChars()
    {
        char c = Char.MinValue;

        while (c < char.MaxValue)
        {
            if (Char.IsWhiteSpace(c))
            {
                yield return c;
            }

            c++;
        }
    }
}

public record Benchmark(string Name, Func<string, Count> Test);

public record struct BenchmarkResult(int Pass, string Benchmark, TimeSpan Duration, Count Counts);

public record struct Count(int Lines, int Words, int Bytes, string File);
