using BenchmarkData;

namespace FileOpenTextReadLineOldBenchmark;

public static class FileOpenTextReadLineOldBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, charCount = 0;
        using var stream = File.OpenText(path);

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
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