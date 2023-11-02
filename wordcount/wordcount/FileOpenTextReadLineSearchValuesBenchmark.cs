using BenchmarkData;

namespace FileOpenTextReadLineSearchValuesBenchmark;

public static class FileOpenTextReadLineSearchValuesBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, charCount = 0;
        using StreamReader stream = File.OpenText(path);

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            lineCount++;
            charCount += line.Length;
            ReadOnlySpan<char> text = line.AsSpan().TrimStart();

            if (text.Length is 0)
            {
                continue;
            }

            int index = 0;
            while ((index = text.IndexOfAny(BenchmarkValues.WhitespaceSearchValuesNoLineBreak)) > 0)
            {
                wordCount++;
                text = text.Slice(index).TrimStart();
            }

            wordCount++;
        }

        return new(lineCount, wordCount, charCount, path);
    }
}
