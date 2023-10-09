using BenchmarkData;

namespace Runner;

public static class PrintTestResults
{
    public static void Print(List<BenchmarkResult> benchmarkResults, int iterations)
    {
        var benchmarks = BenchmarkValues.Benchmarks;

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

        Console.WriteLine("Order by name:");

        foreach (var result in results.OrderBy(r => r.Name))
        {
            Console.WriteLine($"{result.Name}: {result.Average:.###}");
        }

        Console.WriteLine();
        Console.WriteLine("Order by average:");
        
        foreach (var result in results.OrderBy(r => r.Average))
        {
            Console.WriteLine($"{result.Name}: {result.Average:.###}");
        }
    }
}