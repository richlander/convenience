using System.Data;
using System.Runtime.Intrinsics;
using BenchmarkData;

namespace Runner;

public static class Runner
{
    public static Count RunOneFile(string path)
    {
        var count = BenchmarkValues.Benchmark.Test(path);
        Console.WriteLine($"{count.Lines} {count.Words} {count.Bytes} {count.File}");
        return count;
    }

    public static void RunMultiFile(string path)
    {
        int totalLines = 0, totalWords = 0, totalBytes = 0;

        foreach (var file in Directory.EnumerateFiles(path).Order())
        {
            var count = RunOneFile(file);
            totalLines += count.Lines;
            totalWords += count.Words;
            totalBytes += count.Bytes;
        }

        Console.WriteLine($"{totalLines} {totalWords} {totalBytes} total");
    }

    public static void PrintHardwareAcceleration()
    {
        Console.WriteLine($"{nameof(Vector64)}.IsHardwareAccelerated: {Vector64.IsHardwareAccelerated}");
        Console.WriteLine($"{nameof(Vector128)}.IsHardwareAccelerated: {Vector128.IsHardwareAccelerated}");
        Console.WriteLine($"{nameof(Vector256)}.IsHardwareAccelerated: {Vector256.IsHardwareAccelerated}");
        Console.WriteLine($"{nameof(Vector512)}.IsHardwareAccelerated: {Vector512.IsHardwareAccelerated}");
    }
}
