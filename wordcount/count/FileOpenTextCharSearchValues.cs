
using System.Buffers;
using BenchmarkData;

namespace FileOpenTextCharSearchValuesBenchmark;

public static class FileOpenTextCharSearchValuesBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, charCount = 0;
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
                    wordCount++;
                    wasSpace = false;
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

        ArrayPool<char>.Shared.Return(buffer);
        return new(lineCount, wordCount, charCount, path);
    }
}
