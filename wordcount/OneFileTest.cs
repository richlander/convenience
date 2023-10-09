using System.Diagnostics;
using BenchmarkData;

namespace OneFileTest;

public static class OneFileTest
{
    public static void Go(int iterations)
    {
        var benchmarks = BenchmarkValues.Benchmarks;
        var path = BenchmarkValues.FilePath;

        List<BenchmarkResult> benchmarkResults = [];

        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine("***");
            Console.WriteLine($"*** Pass {i} ****************");
            Console.WriteLine("***");

            foreach (var benchmark in benchmarks)
            {
                Console.WriteLine();
                Console.WriteLine($"=/=\\= {benchmark.Name} =/=\\=");

                var stopwatch = Stopwatch.StartNew();
                var counts = benchmark.Test(path);
                stopwatch.Stop();
                benchmarkResults.Add(new(i, benchmark.Name, stopwatch.ElapsedTicks, counts));

                Console.WriteLine();
                Console.WriteLine($"{nameof(Stopwatch.ElapsedTicks)}: {stopwatch.ElapsedTicks};");
                Console.WriteLine($"{counts.Lines} {counts.Words} {counts.Bytes}");
                Console.WriteLine();
            }
        }

        Console.WriteLine();

        Runner.PrintTestResults.Print(benchmarkResults, iterations);
    }
}