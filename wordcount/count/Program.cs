using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using BenchmarkData;
using Counter;

var stopwatch = Stopwatch.StartNew();
string path = args.Length > 0 ? args[0] : "";
ArgumentNullException.ThrowIfNullOrEmpty(path);


if (File.Exists(path))
{
    var count = FileOpenHandleCharSearchValuesBenchmark.Count(path);
    PrintCount(count);
}
else if (Directory.Exists(path))
{
    long lineCount = 0, wordCount = 0, byteCount = 0;

    foreach (var file in Directory.EnumerateFiles(path).Order())
    {
        var count = FileOpenHandleCharSearchValuesBenchmark.Count(file);
        PrintCount(count);

        lineCount += count.Lines;
        wordCount += count.Words;
        byteCount += count.Bytes;
    }

    Console.WriteLine($"{"",2} {lineCount, 6}  {wordCount, 6}  {byteCount, 6} total");
}
else
{
    Console.WriteLine("Please specify a file or directory.");
}

stopwatch.Stop();

Console.WriteLine($"Elapsed time (ms): {stopwatch.ElapsedMilliseconds}");
Console.WriteLine($"Elapsed time (us): {stopwatch.Elapsed.TotalMicroseconds}");

void PrintCount(Count count)
{
    Console.WriteLine($"{"",2} {count.Lines, 6}  {count.Words, 6}  {count.Bytes, 6} {count.File, 6}");
}