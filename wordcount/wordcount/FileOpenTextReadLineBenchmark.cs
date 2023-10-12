
using BenchmarkData;

namespace FileOpenTextReadLineBenchmark;

public static class FileOpenTextReadLineBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, charCount = 0;
        using StreamReader stream = File.OpenText(path);
        char[] space = [' '];

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            lineCount++;
            charCount += line.Length;
            ReadOnlySpan<char> text = line.TrimStart();

            if (text.Length is 0)
            {
                continue;
            }

            int index = 0;
            while ((index = text.IndexOfAny(space)) > 0)
            {
                wordCount++;
                text = text.Slice(index).TrimStart();
            }

            wordCount++;
        }

        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }
}
