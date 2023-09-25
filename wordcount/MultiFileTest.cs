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

        Console.WriteLine();

        Console.WriteLine("Pass,Benchmark,Duration,Lines,Words,Bytes");
        foreach (var item in benchmarkResults)
        {
            var counts = item.Counts;
            Console.WriteLine($"{item.Pass},{item.Benchmark},{item.Duration},{counts.Lines},{counts.Words},{counts.Bytes}");
        }

        Console.WriteLine();
        Console.WriteLine();

        // Remove warmup iterations and outliers (using a TRIMMEAN-like approach)
        var resultDivisor = 1_000_000;
        var warmupIterations = int.Min(6, iterations / 4);
        var warmupSkipCount = warmupIterations * benchmarks.Count;
        var outlierSkipCount = int.Min(2, iterations / 10);
        var resultValues = benchmarkResults.Skip(warmupSkipCount).GroupBy(r => r.Benchmark).Select(g => new {Name=g.Key, Values=g.Select(r => r.Duration).Order().Skip(outlierSkipCount).SkipLast(outlierSkipCount)});
        var results = resultValues.Select(r => new {Name=r.Name, Values=r.Values.ToList(), Average=r.Values.Average()/resultDivisor}).ToList();
        
        var expectedIterations = iterations - warmupIterations - (outlierSkipCount * 2);

        Console.WriteLine($"Total passes: {iterations}");
        Console.WriteLine($"Warmup passes: {warmupIterations}");
        Console.WriteLine($"Outlier passes ignored: {outlierSkipCount * 2}");
        Console.WriteLine($"Measured passes: {results[0].Values.Count}");
        Console.WriteLine();

        foreach (var result in results.OrderBy(r => r.Average))
        {
            Console.WriteLine($"{result.Name}: {result.Average:.##}");

            if (result.Values.Count != expectedIterations)
            {
                Console.WriteLine($"***Warning: iterator count doesn't match. Expected {expectedIterations} and observed {result.Values.Count}.");
            }
        }
    }
}