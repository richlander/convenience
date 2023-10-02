using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using JsonConfig;

List<Benchmark> webBenchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async Task<int> (url) => await JsonSerializerBenchmark.JsonSerializerBenchmark.MakeReportWebAsync(url), null),
    new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), async Task<int> (url) => await JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.MakeReportWebAsync(url), null),
    new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), async Task<int> (url) => await JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.MakeReportWebAsync(url), null),
    new(nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), async Task<int> (url) => await JsonDocumentBenchmark.JsonDocumentBenchmark.MakeReportAsync(url), null),
    new(nameof(Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark), async Task<int> (url) => await Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark.MakeReportWebAsync(url), null),
    new(nameof(Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark), async Task<int> (url) => await Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark.MakeReportWebAsync(url), null),
    new(nameof(Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark), async Task<int> (url) => await Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark.MakeReportWebAsync(url), null),
    new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), async Task<int> (url) => await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.MakeReportWebAsync(url), null),
];

List<Benchmark> localBenchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async Task<int> (path) => await JsonSerializerBenchmark.JsonSerializerBenchmark.MakeReportFileAsync(path), null),
    new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), async Task<int> (path) => await JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.MakeReportFileAsync(path), null),
    new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), async Task<int> (path) => await JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.MakeReportFileAsync(path), null),
    new(nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), async Task<int> (path) => await JsonDocumentBenchmark.JsonDocumentBenchmark.MakeReportFileAsync(path), null),
    new(nameof(Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark), async Task<int> (path) => await Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark.MakeReportFileAsync(path), null),
    new(nameof(Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark), async Task<int> (path) => await Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark.MakeReportFileAsync(path), null),
    new(nameof(Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark), async Task<int> (path) => await Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark.MakeReportFileAsync(path), null),
    new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), null, (path) => NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.MakeReportFile(path)),
];

List<Benchmark> memoryBenchmarks =
[
    new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), null, (json) => JsonSerializerBenchmark.JsonSerializerBenchmark.MakeReportMemory(json)),
    new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), null, (json) => JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.MakeReportMemory(json)),
    new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), null, (json) => JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.MakeReportMemory(json)),
];

var benchmarks = webBenchmarks;
string target = BenchmarkData.RemoteUrl;
string targetJson = "";

int count = args.Length > 0 && int.TryParse(args[0], out int num) ? num : -1;
int index = args.Length > 1 && int.TryParse(args[1], out num) ? num : -1;

if (index > -1)
{
    BenchmarkData.File = BenchmarkData.FakeReleaseJson[index];
}

if (count is -1)
{
    count = 16;
}
else if (count > 300)
{
    benchmarks = memoryBenchmarks;
    target = BenchmarkData.Path;
    targetJson = File.ReadAllText(target);
    count -= 200;
}
else if (count > 200)
{
    benchmarks = localBenchmarks;
    target = BenchmarkData.Path;
    count -= 200;
}
else if (count > 100)
{
    target = BenchmarkData.LocalUrl;
    count -= 100;
}

if (count >= 10)
{
    int iterations = count is 100 ? 1 : count;
    List<BenchmarkResult> benchmarkResults = [];

    for (int i = 0; i < iterations; i++)
    {
        Console.WriteLine("***");
        Console.WriteLine($"*** Pass {i} ****************");
        Console.WriteLine("***");

        foreach (var benchmark in benchmarks)
        {
            Console.WriteLine();
            Console.WriteLine($"*** {benchmark.Name} ***");
            var stopwatch = Stopwatch.StartNew();
            int length = -1;

            if (benchmark.AsyncTest is {})
            {
                length = await benchmark.AsyncTest(target);
            }
            else if (benchmark.Test is {})
            {
                if (!string.IsNullOrEmpty(targetJson))
                {
                    length = benchmark.Test(targetJson);
                }
                else
                {
                    length = benchmark.Test(target);
                }
                
            }
            else
            {
                Console.WriteLine("******No test found******");
            }

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"{nameof(Stopwatch.ElapsedTicks)}: {stopwatch.ElapsedTicks}; JSON Length: {length}");
            benchmarkResults.Add(new(i, benchmark.Name, stopwatch.ElapsedTicks,length));
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
    var ticksDivisor = 1000_000.0;
    var warmupIterations = int.Min(6, iterations / 4);
    var warmupSkipCount = warmupIterations * benchmarks.Count;
    var outlierSkipCount = (int)(iterations * 0.1);
    var resultValues = benchmarkResults.Skip(warmupSkipCount).GroupBy(r => r.Name).Select(g => new {Name=g.Key, Values=g.Select(r => r.Duration).Order().Skip(outlierSkipCount).SkipLast(outlierSkipCount)});
    var results = resultValues.Select(r => new {Name=r.Name, Values=r.Values.ToList(), Average=r.Values.Average() / ticksDivisor}).ToList();
    
    var expectedIterations = iterations - warmupIterations - (outlierSkipCount * 2);

    Console.WriteLine("JSON:");
    Console.WriteLine(target);
    Console.WriteLine();
    Console.WriteLine($"Total passes: {iterations}");
    Console.WriteLine($"Warmup passes: {warmupIterations}");
    Console.WriteLine($"Outlier passes ignored: {outlierSkipCount * 2}");
    Console.WriteLine($"Measured passes: {results[0].Values.Count}");
    Console.WriteLine($"{nameof(Stopwatch.Frequency)}: {Stopwatch.Frequency}");
    Console.WriteLine($"{nameof(ticksDivisor)}: {ticksDivisor}");
    Console.WriteLine();

    foreach (var result in results.OrderBy(r => r.Average))
    {
        Console.WriteLine($"{result.Name}: {result.Average:.###}");

        if (result.Values.Count != expectedIterations)
        {
            Console.WriteLine($"***Warning: iterator count doesn't match. Expected {expectedIterations} and observed {result.Values.Count}.");
        }
    }
}
else if (count < benchmarks.Count)
{
    await RunMemoryBenchmark(benchmarks[count], target);
}
else
{
    Console.WriteLine("Bad input. Try again.");
}


static async Task RunMemoryBenchmark(Benchmark benchmark, string url)
{
    Console.WriteLine($"********{benchmark.Name}");
    var beforeGCCount = GC.CollectionCount(0);
    var beforeWorkingSet = Environment.WorkingSet;
    var beforeAllocatedBytes = GC.GetTotalAllocatedBytes();
    var beforeCompiledMethodCount = JitInfo.GetCompiledMethodCount();
    var beforeCompiledILBytes = JitInfo.GetCompiledILBytes();
    var stopwatch = Stopwatch.StartNew();
    int length = -1;

    if (benchmark.AsyncTest is {})
    {
        length = await benchmark.AsyncTest(url);
    }
    else
    {
        Console.WriteLine("******No test found******");
    }

    stopwatch.Stop();
    var afterGCCount = GC.CollectionCount(0);
    var afterWorkingSet = Environment.WorkingSet;
    var afterAllocatedBytes = GC.GetTotalAllocatedBytes();
    var afterCompiledMethodCount = JitInfo.GetCompiledMethodCount();
    var afterCompiledILBytes = JitInfo.GetCompiledILBytes();

    Console.WriteLine();
    Console.WriteLine("JSON:");
    Console.WriteLine(BenchmarkData.RemoteUrl);
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

public record Benchmark(string Name, Func<string, Task<int>>? AsyncTest, Func<string, int>? Test);

public record BenchmarkResult(int Pass, string Name, long Duration, int Length);
