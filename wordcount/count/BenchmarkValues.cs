using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BenchmarkData;

public static class BenchmarkValues
{
    public static int Size => 4 * 1024;

    public static char[] WhitespaceValues = GetWhiteSpaceChars().ToArray();

    public static SearchValues<char> WhitespaceSearchValues = SearchValues.Create(WhitespaceValues);

    public static IEnumerable<char> GetWhiteSpaceChars()
    {
        for (int i = char.MinValue; i <= char.MaxValue; i++)
        {
            if (char.IsWhiteSpace((char)i)) { yield return (char)i; }
        }
    }

    public static List<Benchmark> Benchmarks => 
    [
        new(nameof(FileOpenHandleCharSearchValuesBenchmark), FileOpenHandleCharSearchValuesBenchmark.FileOpenHandleCharSearchValuesBenchmark.Count),
        new(nameof(FileOpenCharSearchValuesBenchmark), FileOpenCharSearchValuesBenchmark.FileOpenCharSearchValuesBenchmark.Count),
        new(nameof(FileOpenTextCharSearchValuesBenchmark), FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count),
        new(nameof(FileReadLinesBenchmark), FileReadLinesBenchmark.FileReadLinesBenchmark.Count),
        new(nameof(FileOpenHandleAsciiCheatBenchmark), FileOpenHandleAsciiCheatBenchmark.FileOpenHandleAsciiCheatBenchmark.Count),
    ];

}

public record struct Count(long Lines, long Words, long Bytes, string File);

public record Benchmark(string Name, Func<string, Count> Test);
