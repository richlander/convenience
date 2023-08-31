using System.Diagnostics;

List<Benchmark> benchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async () => await JsonSerializerBenchmark.JsonSerializerBenchmark.Run()),
    new(nameof(JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark), async () => await JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run()),
    new(nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), async () => await JsonDocumentBenchmark.JsonDocumentBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark), async () => await Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run()),
    new(nameof(Utf8JsonReaderWriterRawBenchmark.Utf8JsonReaderWriterRawBenchmark), async () => await Utf8JsonReaderWriterRawBenchmark.Utf8JsonReaderWriterRawBenchmark.Run()),
    new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), async () => await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.Run()),
];

int index = 4;

if (args is {Length: >0} && args[0] is {Length: > 0})
{
    index = int.Parse(args[0]);
}

var benchmark = benchmarks[index];
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

public record Benchmark(string Name, Func<Task> Test);
