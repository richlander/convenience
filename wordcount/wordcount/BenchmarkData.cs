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

    public static int Size => 4 * 1024;

    public static char[] WhitespaceValues = GetWhiteSpaceChars().ToArray();

    public static char[] WhitespaceValuesNoLineBreak = GetWhiteSpaceChars(true).ToArray();

    public static List<Benchmark> Benchmarks => 
        [
            new(nameof(FileOpenHandleCharSearchValuesBenchmark), FileOpenHandleCharSearchValuesBenchmark.FileOpenHandleCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenCharSearchValuesBenchmark), FileOpenCharSearchValuesBenchmark.FileOpenCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenHandleCharBenchmark), FileOpenHandleCharBenchmark.FileOpenHandleCharBenchmark.Count),
            new(nameof(FileOpenHandleRuneBenchmark), FileOpenHandleRuneBenchmark.FileOpenHandleRuneBenchmark.Count),
            new(nameof(FileOpenHandleAsciiCheatBenchmark), FileOpenHandleAsciiCheatBenchmark.FileOpenHandleAsciiCheatBenchmark.Count),
            new(nameof(FileOpenTextCharBenchmark), FileOpenTextCharBenchmark.FileOpenTextCharBenchmark.Count),
            new(nameof(FileOpenTextCharLinesBenchmark), FileOpenTextCharLinesBenchmark.FileOpenTextCharLinesBenchmark.Count),
            new(nameof(FileOpenTextCharSearchValuesBenchmark), FileOpenTextCharSearchValuesBenchmark.FileOpenTextCharSearchValuesBenchmark.Count),
            new(nameof(FileOpenTextCharIndexOfAnyBenchmark), FileOpenTextCharIndexOfAnyBenchmark.FileOpenTextCharIndexOfAnyBenchmark.Count),
            new(nameof(FileOpenTextReadLineBenchmark), FileOpenTextReadLineBenchmark.FileOpenTextReadLineBenchmark.Count),
            new(nameof(FileOpenTextReadLineSearchValuesBenchmark), FileOpenTextReadLineSearchValuesBenchmark.FileOpenTextReadLineSearchValuesBenchmark.Count),
            new(nameof(FileReadLinesBenchmark), FileReadLinesBenchmark.FileReadLinesBenchmark.Count),
            new(nameof(FileReadAllLinesBenchmark), FileReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count),
        ];

    public static Benchmark Benchmark { get; set; } = Benchmarks[0];

    public static SearchValues<char> WhitespaceSearchValues = SearchValues.Create(WhitespaceValues);

    public static SearchValues<char> WhitespaceSearchValuesNoLineBreak = SearchValues.Create(WhitespaceValuesNoLineBreak);

    public static IEnumerable<char> GetWhiteSpaceChars(bool skipControl = false)
    {
        for (int i = char.MinValue; i <= char.MaxValue; i++)
        {
            if (skipControl && char.IsControl((char)i)) {continue;}
            if (char.IsWhiteSpace((char)i)) { yield return (char)i; }
        }
    }
}

public record Benchmark(string Name, Func<string, Count> Test);

public record struct Count(long Lines, long Words, long Bytes, string File);
