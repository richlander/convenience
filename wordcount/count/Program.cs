using WordCount;

string path = args.Length > 0 ? args[0] : "";

if (File.Exists(path))
{
    var count = FileOpenHandleSearchValues.Count(path);
    PrintCount(count);
}
else if (Directory.Exists(path))
{
    int lineCount = 0, wordCount = 0, byteCount = 0;

    foreach (var file in Directory.EnumerateFiles(path).Order())
    {
        var count = FileOpenHandleSearchValues.Count(file);
        PrintCount(count);

        lineCount += count.Lines;
        wordCount += count.Words;
        byteCount += count.Bytes;
    }

    Console.WriteLine($"Totals: {lineCount} {wordCount} {byteCount}");
}
else
{
    Console.WriteLine("Please specify a file or directory.");
}

void PrintCount(Count count)
{
    Console.WriteLine($"{"",2} {count.Lines, -7} {count.Words, -7} {count.Bytes, -7} {count.File, -5}");
}