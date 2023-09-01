using System.Diagnostics;
using System.Net;

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
    List<string> log = [];
    log.Add("Pass,Benchmark,Duration,JSONLength");
    for (int i = 0; i < 16; i ++)
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
            log.Add($"{i},{benchmark.Name},{stopwatch.ElapsedMilliseconds},{length}");
            Console.WriteLine();
            Console.WriteLine($"{nameof(Stopwatch.ElapsedMilliseconds)}: {stopwatch.ElapsedMilliseconds}; JSON Length: {length}");
        }
    }

    Console.WriteLine();

    foreach (var line in log)
    {
        Console.WriteLine(line);
    }
}
else
{
    await RunFullBenchmark(benchmarks[index]);
}


static async Task RunFullBenchmark(Benchmark benchmark)
{
    var stopwatch = Stopwatch.StartNew();
    GC.Collect();
    GC.Collect();
    var beforeInfo = GC.GetGCMemoryInfo();

    Console.WriteLine($"********{benchmark.Name}");
    stopwatch.Restart();
    await benchmark.Test();
    stopwatch.Stop();
    GC.Collect();
    var afterInfo = GC.GetGCMemoryInfo();


    var heapDiff = afterInfo.HeapSizeBytes - beforeInfo.HeapSizeBytes;
    Console.WriteLine($"Before:{nameof(GCMemoryInfo.HeapSizeBytes)}: {beforeInfo.HeapSizeBytes}");
    Console.WriteLine($"After:{nameof(GCMemoryInfo.HeapSizeBytes)}: {afterInfo.HeapSizeBytes}");
    Console.WriteLine($"Diff:{nameof(GCMemoryInfo.HeapSizeBytes)}: {heapDiff}");
    Console.WriteLine($"{nameof(Stopwatch.ElapsedMilliseconds)}: {stopwatch.ElapsedMilliseconds}");
}

public record Benchmark(string Name, Func<Task<int>> Test);
