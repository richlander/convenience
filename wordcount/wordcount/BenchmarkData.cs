using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BenchmarkData;

public static class BenchmarkValues
{

    public static string FilePath => Path.Combine(DirectoryPath, "clarissa_volume1.txt");

    public static string ShortFilePath => Path.Combine(DirectoryPath, "summary.md");

    public static string DirectoryPath => Path.Combine(Path.GetDirectoryName(typeof(BenchmarkValues).Assembly.Location)!, "Clarissa_Harlowe");

    public static int Size => 16 * 1024;

    public static List<Benchmark> Benchmarks => 
        [
            new(nameof(FileOpenCharsBenchmark), FileOpenCharsBenchmark.FileOpenCharsBenchmark.Count),
            new(nameof(FileOpenHandleCharSearchValuesBenchmark), FileOpenHandleCharSearchValuesBenchmark.FileOpenHandleCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenHandleRuneBenchmark), FileOpenHandleRuneBenchmark.FileOpenHandleRuneBenchmark.Count),
            new(nameof(FileOpenTextCharBenchmark), FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count),
            new(nameof(FileOpenTextCharSearchValuesBenchmark), FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenTextReadLineBenchmark), FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count),
            new(nameof(FileOpenTextReadLineSearchValuesBenchmark), FileOpenTextReadLineSearchValuesBenchmark.FileOpenTextReadLineSearchValuesBenchmark.Count),
            new(nameof(FileReadLinesBenchmark), FileReadLinesBenchmark.FileReadLinesBenchmark.Count),
            new(nameof(FileReadAllLinesBenchmark), FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count),
            new(nameof(FileOpenHandleAsciiCheatBenchmark), FileOpenHandleAsciiCheatBenchmark.FileOpenHandleAsciiCheatBenchmark.Count),
        ];

    public static Benchmark Benchmark { get; set; } = Benchmarks[0];

    public static SearchValues<char> WhitespaceSearch = SearchValues.Create(GetWhiteSpaceChars().ToArray());

    public static SearchValues<char> WhitespaceSearchNoCRLF = SearchValues.Create(GetWhiteSpaceChars().SkipWhile(c => c is '\n' or '\r').ToArray());

    public static IEnumerable<char> GetWhiteSpaceChars()
    {
        for (int i = char.MinValue; i <= char.MaxValue; i++)
        {
            if (char.IsWhiteSpace((char)i)) { yield return (char)i; }
        }
    }
}

public record Benchmark(string Name, Func<string, Count> Test);

public record struct Count(long Lines, long Words, long Bytes, string File);
