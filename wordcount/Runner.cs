using System.Data;
using BenchmarkData;

namespace Runner;

public static class Runner
{
    public static Count RunOneFile()
    {
        var count = FileOpenSearchValuesBenchmark.FileOpenSearchValuesBenchmark.Count(BenchmarkData.BenchmarkValues.FilePath);
        Console.WriteLine($"{count.Lines} {count.Words} {count.Bytes} {count.File}");
        return count;
    }

    public static void RunMultiFile()
    {
        List<Count> counts = [];

        foreach (var file in Directory.EnumerateFiles(BenchmarkData.BenchmarkValues.DirectoryPath))
        {
            var count = RunOneFile();
            counts.Add(count);
        }

        PrintCounts(counts);
    }

    public static void PrintCounts(List<Count> counts)
    {
        foreach (var count in counts)
        {
            Console.WriteLine($"{count.Lines} {count.Words} {count.Bytes} {count.File}");
        }

        var totalLines = counts.Sum(c => c.Lines);
        var totalWords = counts.Sum(c => c.Words);
        var totalBytes = counts.Sum(c => c.Bytes);

        Console.WriteLine($"{totalLines} {totalWords} {totalBytes} total");
    }
}
