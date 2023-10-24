
using System.Buffers;
using BenchmarkData;

namespace FileOpenTextCharBenchmark;

public static class FileOpenTextCharBenchmark
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
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                }

                chars = chars.Slice(1);
            }
        }

        ArrayPool<char>.Shared.Return(buffer);
        return new(lineCount, wordCount, charCount, path);
    }
}
