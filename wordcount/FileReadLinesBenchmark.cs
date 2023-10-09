
using BenchmarkData;

namespace FileReadLinesBenchmark;

public static class FileReadLinesBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineCount++;
            charCount += line.Length;
            bool wasSpace = true;

            foreach (var c in line)
            {
                bool isSpace = Char.IsWhiteSpace(c);

                if (!isSpace && wasSpace)
                {
                    wordCount++;
                }

                wasSpace = isSpace;
            }
        }

        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }
}