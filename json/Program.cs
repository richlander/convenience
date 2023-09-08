using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

List<Benchmark> benchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async Task<int> () => await JsonSerializerBenchmark.JsonSerializerBenchmark.Run()),
    new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), async Task<int> () => await JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.Run()),
    new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), async Task<int> () => await JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.Run()),
    new(nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), async Task<int> () => await JsonDocumentBenchmark.JsonDocumentBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark), async Task<int> () => await Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark), async Task<int> () => await Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark), async Task<int> () => await Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark.Run()),
    new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), async Task<int> () => await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.Run()),
];

int index = 5;

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
    var beforeGCCount = GC.CollectionCount(0);
    var beforeWorkingSet = Environment.WorkingSet;
    var beforeAllocatedBytes = GC.GetTotalAllocatedBytes();
    var beforeCompiledMethodCount = JitInfo.GetCompiledMethodCount();
    var beforeCompiledILBytes = JitInfo.GetCompiledILBytes();
    var stopwatch = Stopwatch.StartNew();
    var length = await benchmark.Test();
    stopwatch.Stop();
    var afterGCCount = GC.CollectionCount(0);
    var afterWorkingSet = Environment.WorkingSet;
    var afterAllocatedBytes = GC.GetTotalAllocatedBytes();
    var afterCompiledMethodCount = JitInfo.GetCompiledMethodCount();
    var afterCompiledILBytes = JitInfo.GetCompiledILBytes();

    Console.WriteLine();
    Console.WriteLine($"{nameof(Environment.WorkingSet)}: {afterWorkingSet - beforeWorkingSet}");
    Console.WriteLine($"{nameof(GC.GetTotalAllocatedBytes)}: {afterAllocatedBytes - beforeAllocatedBytes}");
    Console.WriteLine($"{nameof(JitInfo.GetCompiledMethodCount)}: {afterCompiledMethodCount - beforeCompiledMethodCount}");
    Console.WriteLine($"{nameof(JitInfo.GetCompiledILBytes)}: {afterCompiledILBytes - beforeCompiledILBytes}");    
    Console.WriteLine($"{nameof(GC.CollectionCount)}: {afterGCCount - beforeGCCount }");

    Console.WriteLine();
    Console.WriteLine($"Length: {length}");
    Console.WriteLine($"{nameof(Stopwatch.ElapsedMilliseconds)}: {stopwatch.ElapsedMilliseconds}");

    Console.WriteLine();
    Console.WriteLine($"{nameof(RuntimeInformation.OSArchitecture)}: {RuntimeInformation.OSArchitecture}");
    Console.WriteLine($"{nameof(RuntimeInformation.FrameworkDescription)}: {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"{nameof(RuntimeInformation.OSDescription)}: {RuntimeInformation.OSDescription}");
}

public record Benchmark(string Name, Func<Task<int>> Test);

public record BenchmarkResult(int Pass, string Name, long Duration, int Length);
