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
            new(nameof(FileOpenHandleBenchmark), FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count),
            new(nameof(FileOpenHandleMultiByteBenchmark), FileOpenHandleMultiByteBenchmark.FileOpenHandleMultiByteBenchmark.Count),
            new(nameof(FileOpenBenchmark), FileOpenBenchmark.FileOpenBenchmark.Count),
            new(nameof(FileOpenTextCharBenchmark), FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count),
            new(nameof(FileOpenTextCharSearchValuesBenchmark), FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenTextReadLineBenchmark), FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count),
            new(nameof(FileReadLinesBenchmark), FileOpenBenchmark.FileOpenBenchmark.Count),
            new(nameof(FileReadAllLinesBenchmark), FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count),
        ];

    public static Benchmark Benchmark { get; set; } = Benchmarks[0];

    public static SearchValues<byte> WhitespaceSearchAscii = SearchValues.Create((ReadOnlySpan<byte>)[9, 10, 11, 12, 13, 32, 194, 225, 226, 227]);

    public static SearchValues<char> WhitespaceSearch = SearchValues.Create(GetWhiteSpaceChars().AsSpan());

    public static WhiteSpaceValues GetWhiteSpaceChars()
    {
        WhiteSpaceValues whitespace = new();
        char c = Char.MinValue;
        int index = 0;

        while (c < Char.MaxValue)
        {
            if (Char.IsWhiteSpace(c))
            {
                whitespace[index++] = c;
            }

            c++;
        }

        return whitespace;
    }
}

public record Benchmark(string Name, Func<string, Count> Test);

public record struct BenchmarkResult(int Pass, string Benchmark, TimeSpan Duration, Count Counts);

public record struct Count(int Lines, int Words, int Bytes, string File);

[InlineArray(Length)]
public struct WhiteSpaceValues
{
    private const int Length = 25;
    char _element;

    [UnscopedRef]
    public Span<char> AsSpan() => MemoryMarshal.CreateSpan<char>(ref _element, Length);
}
