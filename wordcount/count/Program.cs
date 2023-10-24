using BenchmarkData;


string path = args.Length > 0 ? args[0] : "";

if (File.Exists(path))
{
    var count = FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count(path);
    PrintCount(count);
}
else if (Directory.Exists(path))
{
    int lineCount = 0, wordCount = 0, byteCount = 0;

    foreach (var file in Directory.EnumerateFiles(path).Order())
    {
        var count = FileOpenHandleBenchmark.FileOpenHandleBenchmark.Count(file);
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

void PrintCount(Count count)
{
    Console.WriteLine($"{"",2} {count.Lines, 6}  {count.Words, 6}  {count.Bytes, 6} {count.File, 6}");
}