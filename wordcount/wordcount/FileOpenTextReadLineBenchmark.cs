
using BenchmarkData;

namespace FileOpenTextReadLineBenchmark;

public static class FileOpenTextReadLineBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, charCount = 0;
        using StreamReader stream = File.OpenText(path);
        char[] a = [' '];

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            lineCount++;
            charCount += line.Length;
            bool wasSpace = true;
            ReadOnlySpan<char> text = line; 

            while (text.Length > 0)
            {
                bool isSpace = Char.IsWhiteSpace(text[0]);

                if (!isSpace && wasSpace)
                {
                    wordCount++;
                }

                int index = text.IndexOfAny(a);
                text = index > 0 ? text.Slice(index) : text.Slice(1);

                wasSpace = isSpace;
            }
        }

        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }
}
