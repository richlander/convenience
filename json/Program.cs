using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Serialization;

List<Benchmark> benchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async Task<int> () => await JsonSerializerBenchmark.JsonSerializerBenchmark.Run()),
    new(nameof(JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark), async Task<int> () => await JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run()),
    new(nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), async Task<int> () => await JsonDocumentBenchmark.JsonDocumentBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark), async Task<int> () => await Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterRawBenchmark.Utf8JsonReaderWriterRawBenchmark), async Task<int> () => await Utf8JsonReaderWriterRawBenchmark.Utf8JsonReaderWriterRawBenchmark.Run()),
    new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), async Task<int> () => await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.Run()),
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
    int iterations = index;
    List<BenchmarkResult> benchmarkResults = [];

    for (int i = 0; i < iterations; i ++)
    {
        Console.WriteLine("***");
        Console.WriteLine($"*** Pass {i} ****************");
        Console.WriteLine("***");

        foreach (var benchmark in benchmarks)
        {
            Console.WriteLine();
            Console.WriteLine($"*** {benchmark.Name} ***");
            var stopwatch = Stopwatch.StartNew();
            var length = await benchmark.Test();
            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"{nameof(Stopwatch.ElapsedMilliseconds)}: {stopwatch.ElapsedMilliseconds}; JSON Length: {length}");
            benchmarkResults.Add(new(i, benchmark.Name, stopwatch.ElapsedMilliseconds,length));
        }
    }

    Console.WriteLine();

    Console.WriteLine("Pass,Benchmark,Duration,JSONLength");
    foreach (var item in benchmarkResults)
    {
        Console.WriteLine($"{item.Pass},{item.Name},{item.Duration},{item.Length}");
    }

    Console.WriteLine();
    Console.WriteLine();

    // Remove warmup iterations and outliers (using a TRIMMEAN-like approach)
    var warmupIterations = int.Min(6, iterations / 4);
    var warmupSkipCount = warmupIterations * benchmarks.Count;
    var outlierSkipCount = (int)(iterations * 0.1);
    var resultValues = benchmarkResults.Skip(warmupSkipCount).GroupBy(r => r.Name).Select(g => new {Name=g.Key, Values=g.Select(r => r.Duration).Order().Skip(outlierSkipCount).SkipLast(outlierSkipCount)});
    var results = resultValues.Select(r => new {Name=r.Name, Values=r.Values.ToList(), Average=r.Values.Average()}).ToList();
    
    var expectedIterations = iterations - warmupIterations - (outlierSkipCount * 2);

    Console.WriteLine($"Total passes: {iterations}");
    Console.WriteLine($"Warmup passes: {warmupIterations}");
    Console.WriteLine($"Outlier passes ignored: {outlierSkipCount * 2}");
    Console.WriteLine($"Measured passes: {results[0].Values.Count}");
    Console.WriteLine();

    foreach (var result in results.OrderBy(r => r.Average))
    {
        Console.WriteLine($"{result.Name}: {result.Average:.#}");

        if (result.Values.Count != expectedIterations)
        {
            Console.WriteLine($"***Warning: iterator count doesn't match. Expected {expectedIterations} and observed {result.Values.Count}.");
        }
    }
}
else if (index < benchmarks.Count)
{
    await RunMemoryBenchmark(benchmarks[index]);
}
else
{
    Console.WriteLine("Bad input. Try again.");
}


static async Task RunMemoryBenchmark(Benchmark benchmark)
{
    Console.WriteLine($"********{benchmark.Name}");
    var beforeCount = GC.CollectionCount(0);
    var before = Environment.WorkingSet;
    var stopwatch = Stopwatch.StartNew();
    var length = await benchmark.Test();
    stopwatch.Stop();
    var after = Environment.WorkingSet;
    var afterCount = GC.CollectionCount(0);
    Console.WriteLine($"Length: {length}");
    Console.WriteLine($"{nameof(Environment.WorkingSet)}: {after - before}");
    Console.WriteLine($"{nameof(GC.CollectionCount)}: {afterCount - beforeCount }");
    Console.WriteLine($"{nameof(Stopwatch.ElapsedMilliseconds)}: {stopwatch.ElapsedMilliseconds}");
}

public record Benchmark(string Name, Func<Task<int>> Test);

public record BenchmarkResult(int Pass, string Name, long Duration, int Length);
