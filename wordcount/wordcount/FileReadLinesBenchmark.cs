using BenchmarkData;

namespace FileReadLinesBenchmark;

public static class FileReadLinesBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, charCount = 0;

        foreach (string line in File.ReadLines(path))
        {
            lineCount++;
            charCount += line.Length;
            bool wasSpace = true;

            foreach (char c in line)
            {
                bool isSpace = char.IsWhiteSpace(c);

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
