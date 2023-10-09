using System.Diagnostics;
using BenchmarkData;

namespace MultiFileTest;

public static class MultiFileTest
{
    public static void Go(int iterations)
    {
        List<BenchmarkResult> benchmarkResults = [];
        var benchmarks = BenchmarkValues.Benchmarks;

        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine("***");
            Console.WriteLine($"*** Pass {i} ****************");
            Console.WriteLine("***");

            foreach (var benchmark in benchmarks)
            {
                Console.WriteLine();
                Console.WriteLine($"=/=\\= {benchmark.Name} =/=\\=");
                List<Count> countsList = [];
                Count sum = new();

                var stopwatch = Stopwatch.StartNew();

                foreach (var file in Directory.EnumerateFiles(BenchmarkValues.DirectoryPath))
                {
                    var counts = benchmark.Test(file);
                    countsList.Add(counts);
                    sum.Lines += counts.Lines;
                    sum.Words += counts.Words;
                    sum.Bytes += counts.Bytes;
                }

                stopwatch.Stop();
                benchmarkResults.Add(new(i, benchmark.Name, stopwatch.ElapsedTicks, sum));

                Console.WriteLine();
                Console.WriteLine($"{nameof(Stopwatch.ElapsedTicks)}: {stopwatch.ElapsedTicks};");
                Console.WriteLine($"{sum.Lines} {sum.Words} {sum.Bytes}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("hi");
        Console.ReadLine();

        Runner.PrintTestResults.Print(benchmarkResults, iterations);
    }
}