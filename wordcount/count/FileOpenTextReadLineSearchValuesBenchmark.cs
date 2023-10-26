
using BenchmarkData;

namespace FileOpenTextReadLineSearchValuesBenchmark;

public static class FileOpenTextReadLineSearchValuesBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, charCount = 0;
        bool wasSpace = true;
        using StreamReader stream = File.OpenText(path);
        ReadOnlySpan<char> whitespaceChars = BenchmarkData.BenchmarkValues.GetWhiteSpaceChars().ToArray();

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            lineCount++;
            charCount += line.Length;
            ReadOnlySpan<char> chars = line;

            while (chars.Length > 0)
            {
                if (char.IsWhiteSpace(chars[0]))
                {
                    if (chars[0] is '\n')
                    {
                        lineCount++;                      
                    }

                    wasSpace = true;
                    chars = chars.Slice(1);
                    continue;
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                    chars = chars.Slice(1);
                }

                int index = chars.IndexOfAny(BenchmarkValues.WhitespaceSearch);

                if (index > -1)
                {
                    if (chars[index] is '\n')
                    {
                        lineCount++;       
                    }

                    wasSpace = true;
                    chars = chars.Slice(index + 1);
                }
                else
                {
                    wasSpace = false;
                    chars = [];
                }
            }
        }

        return new(lineCount, wordCount, charCount, path);
    }
}
