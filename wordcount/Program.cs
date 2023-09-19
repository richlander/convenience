using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkData;

var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? throw new Exception("Directory could not be found");  
var path = Path.Combine(dir, BenchmarkValues.FilePath);

List<Benchmark> benchmarks =
[
    new(nameof(ReadAllLinesBenchmark), ReadAllLinesBenchmark.FileReadAllLinesBenchmark.Count),
    new(nameof(ReadLinesBenchmark), FileOpenBenchmark.FileOpenBenchmark.Count),
    new(nameof(FileOpenTextBenchmark), FileOpenTextBenchmark.FileOpenTextBenchmark.Count),
    new(nameof(FileOpenBenchmark), FileOpenBenchmark.FileOpenBenchmark.Count),
    new(nameof(FileOpenSearchValuesBenchmark), FileOpenSearchValuesBenchmark.FileOpenSearchValuesBenchmark.Count),
    new(nameof(FileOpenSToubBenchmark), FileOpenSToubBenchmark.FileOpenSToubBenchmark.Count),
    new(nameof(FileOpenSearchValuesSToubBenchmark), FileOpenSearchValuesSToubBenchmark.FileOpenSearchValuesSToubBenchmark.Count),
    new(nameof(FileOpenHandleBenchmark), FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count),
];

int index = -1;

if (args is {Length: >0} && args[0] is {Length: > 0})
{
    index = int.Parse(args[0]);
}

if (index is -1)
{
    index = 16;
}

if (index >= 10)
{
    int iterations = index is 100 ? 1 : index;
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

// static void PrintCounts(List<BenchmarkResult> counts)
// {
//     foreach (var count in counts)
//     {
//         Console.WriteLine($"{count.Line} {count.Word} {count.Bytes} {count.File}");
//     }

//     var totalLines = counts.Sum(c => c.Line);
//     var totalWords = counts.Sum(c => c.Word);
//     var totalBytes = counts.Sum(c => c.Bytes);

//     if (counts.Count > 1)
//     {
//         Console.WriteLine($"{totalLines} {totalWords} {totalBytes} total");
//     }
// }

    // public static void PrintCounts(BenchmarkResult counts)
    // {
    //     Console.WriteLine($"{counts.Line} {counts.Word} {counts.Bytes} {counts.File}");
    // }

    // public static void PrintMultipleCounts(IEnumerable<BenchmarkResult> countsSet)
    // {
    //     foreach (var counts in countsSet)
    //     {
    //         PrintCounts(counts);
    //     }
    // }
