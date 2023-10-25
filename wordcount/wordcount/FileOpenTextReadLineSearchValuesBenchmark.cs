
using BenchmarkData;

namespace FileOpenTextReadLineSearchValuesBenchmark;

public static class FileOpenTextReadLineSearchValuesBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, charCount = 0;
        using StreamReader stream = File.OpenText(path);

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            lineCount++;
            charCount += line.Length;
            ReadOnlySpan<char> text = line.TrimStart();
            int index = 0;

            if (text.Length is 0)
            {
                continue;
            }

            while ((index = text.IndexOfAny(BenchmarkValues.WhitespaceSearch)) > 0)
            {
                wordCount++;
                text = text.Slice(index).TrimStart();
            }

            wordCount++;
        }

        return new(lineCount, wordCount, charCount, path);
    }
}
