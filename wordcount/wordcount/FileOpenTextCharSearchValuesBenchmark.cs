
using System.Buffers;
using BenchmarkData;

namespace FileOpenTextCharSearchValuesBenchmark;

public static class FileOpenTextCharSearchValuesBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, charCount = 0;
        bool wasSpace = true;

        char[] buffer = ArrayPool<char>.Shared.Rent(BenchmarkValues.Size);
        using var stream = File.OpenText(path);

        int count = 0;
        while ((count = stream.Read(buffer)) > 0)
        {
            charCount += count;
            Span<char> chars = buffer.AsSpan(0, count);

            while (chars.Length > 0)
            {
                char c = chars[0];

                if (char.IsWhiteSpace(c))
                {
                    if (c is ' ')
                    {
                        wasSpace = true;
                    }
                    else if (c is '\n')
                    {
                        wasSpace = true;
                        lineCount++;                      
                    }
                    else if (c is '\r')
                    {
                    }
                    else
                    {
                        wasSpace = true;
                    }

                    chars = chars.Slice(1);
                    continue;
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                }

                if (chars.Length > 16)
                {
                    int index = chars.Slice(1, 16).IndexOfAny(BenchmarkValues.WhitespaceSearch);

                    if (index > -1)
                    {
                        if (chars[index + 1] is '\n')
                        {
                            lineCount++;       
                        }

                        wasSpace = true;
                        chars = chars.Slice(index + 2);
                        continue;
                    }
                }

                chars = chars.Slice(1);
            }
        }

        ArrayPool<char>.Shared.Return(buffer);
        return new(lineCount, wordCount, charCount, path);
    }
}
