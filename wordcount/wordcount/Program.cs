int iterations = args.Length > 0 && int.TryParse(args[0], out int val) ? val : 0;
int index = args.Length > 1 && int.TryParse(args[1], out int val2) ? val2 : -1;

if (iterations is 10)
{
    BenchmarkDotNet.Running.BenchmarkRunner.Run<BenchmarkTests.BenchmarkTests>();
    return;
}

if (index > -1)
{
    BenchmarkData.BenchmarkValues.Benchmark = BenchmarkData.BenchmarkValues.Benchmarks[index];
}

if (iterations is 2)
{
    Runner.Runner.PrintHardwareAcceleration();
}
else if (iterations is 1)
{
    Runner.Runner.RunOneFile(BenchmarkData.BenchmarkValues.FilePath);
}
else
{
    Runner.Runner.RunMultiFile(BenchmarkData.BenchmarkValues.DirectoryPath);
}
