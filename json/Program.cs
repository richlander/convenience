using System.Diagnostics;
using System.Net;

List<Benchmark> benchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async () => await JsonSerializerBenchmark.JsonSerializerBenchmark.Run()),
    new(nameof(JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark), async () => await JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run()),
    new(nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), async () => await JsonDocumentBenchmark.JsonDocumentBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark), async () => await Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterRawBenchmark.Utf8JsonReaderWriterRawBenchmark), async () => await Utf8JsonReaderWriterRawBenchmark.Utf8JsonReaderWriterRawBenchmark.Run()),
    new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), async () => await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.Run()),
];

int index = -1;

if (args is {Length: >0} && args[0] is {Length: > 0})
{
    index = int.Parse(args[0]);
}

if (index is -1)
{
    Console.WriteLine("First pass");

    foreach (var benchmark in benchmarks)
    {
        await RunMiniBenchmark(benchmark);
    }

    Console.WriteLine("Second pass");

    foreach (var benchmark in benchmarks)
    {
        await RunMiniBenchmark(benchmark);
    }

    Console.WriteLine("Third pass");

    foreach (var benchmark in benchmarks)
    {
        await RunMiniBenchmark(benchmark);
    }
}
else
{
    await RunFullBenchmark(benchmarks[index]);
}

static async Task RunMiniBenchmark(Benchmark benchmark)
{
    Console.WriteLine($"********{benchmark.Name}");
    var stopwatch = Stopwatch.StartNew();
    await benchmark.Test();
    stopwatch.Stop();
    Console.WriteLine($"{nameof(Stopwatch.ElapsedMilliseconds)}: {stopwatch.ElapsedMilliseconds}");
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

public record Benchmark(string Name, Func<Task> Test);
