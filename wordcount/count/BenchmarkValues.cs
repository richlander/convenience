using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BenchmarkData;

public static class BenchmarkValues
{
    public static int Size => 16 * 1024;

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

        public static List<Benchmark> Benchmarks => 
        [
            new(nameof(FileOpenHandleRuneBenchmark), FileOpenHandleRuneBenchmark.FileOpenHandleRuneBenchmark.Count),
            new(nameof(FileOpenHandleAsciiCheatBenchmark), FileOpenHandleAsciiCheatBenchmark.FileOpenHandleAsciiCheatBenchmark.Count),
            new(nameof(FileOpenTextCharSearchValuesBenchmark), FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenTextReadLineSearchValuesBenchmark), FileOpenTextReadLineSearchValuesBenchmark.FileOpenTextReadLineSearchValuesBenchmark.Count),
            new(nameof(FileReadLinesBenchmark), FileReadLinesBenchmark.FileReadLinesBenchmark.Count),
        ];

}

public record struct Count(long Lines, long Words, long Bytes, string File);

public record Benchmark(string Name, Func<string, Count> Test);
