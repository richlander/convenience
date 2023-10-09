using BenchmarkDotNet.Running;

int iterations = args.Length > 0 && int.TryParse(args[0], out int val) ? val : 0;

if (iterations > 200)
{
    MultiFileTest.MultiFileTest.Go(iterations - 200);
}
else if (iterations > 100)
{
    OneFileTest.OneFileTest.Go(iterations- 100);
}
else if (iterations is 2)
{
    Runner.Runner.PrintHardwareAcceleration();
}
else if (iterations is 10)
{
    BenchmarkRunner.Run<BenchmarkTests.BenchmarkTests>();
}
else if (iterations is 1)
{
    Runner.Runner.RunMultiFile(BenchmarkData.BenchmarkValues.DirectoryPath);
}
else
{
    Runner.Runner.RunOneFile(BenchmarkData.BenchmarkValues.FilePath);
}
