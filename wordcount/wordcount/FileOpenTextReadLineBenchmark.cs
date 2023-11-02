using BenchmarkData;

namespace FileOpenTextReadLineBenchmark;

public static class FileOpenTextReadLineBenchmark
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
            bool wasSpace = true;

            for (int i = 0; i < line.Length; i++)
            {
                bool isSpace = char.IsWhiteSpace(line[i]);

                if (!isSpace && wasSpace)
                {
                    wordCount++;
                }

                wasSpace = isSpace;
            }
        }

        return new(lineCount, wordCount, charCount, path);
    }
}
