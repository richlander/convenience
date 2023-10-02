using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using JsonBenchmark;

int count = args.Length > 0 && int.TryParse(args[0], out int num) ? num : -1;
int index = args.Length > 1 && int.TryParse(args[1], out num) ? num : -1;

if (index > -1)
{
    Benchmark.File = Benchmark.FakeReleaseJson[index];
}

var benchmarks = Benchmark.WebBenchmarks;
string target = Benchmark.RemoteUrl;
string targetJson = "";

if (count is -1)
{
    count = 16;
}
else if (count > 300)
{
    benchmarks = Benchmark.MemoryBenchmarks;
    target = Benchmark.Path;
    targetJson = File.ReadAllText(target);
    count -= 300;
}
else if (count > 200)
{
    benchmarks = Benchmark.FileBenchmarks;
    target = Benchmark.Path;
    count -= 200;
}
else if (count > 100)
{
    target = Benchmark.LocalUrl;
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

    string kind = targetJson.Length > 0 ? " (string)" : "";
    Console.WriteLine($"JSON{kind}:");
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


static async Task RunMemoryBenchmark(BenchmarkInfo benchmark, string url)
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
    Console.WriteLine(Benchmark.RemoteUrl);
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
