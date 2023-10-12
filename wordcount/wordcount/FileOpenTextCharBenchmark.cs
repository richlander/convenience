
using System.Buffers;
using BenchmarkData;

namespace FileOpenTextCharBenchmark;

public static class FileOpenTextCharBenchmark
{
    public static Count Count(string path)
    {
        const char NEWLINE = '\n';
        const char CARRIAGE_RETURN = '\r';
        const char SPACE = ' ';
        ReadOnlySpan<char> searchValues = [SPACE, NEWLINE];

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
                if (chars[0] is SPACE)
                {
                    wasSpace = true;
                    chars = chars.Slice(1);
                    continue;
                }
                else if (chars[0] is CARRIAGE_RETURN)
                {
                    chars = chars.Slice(1);
                    continue;
                }
                else if (chars[0] is NEWLINE)
                {
                    wasSpace = true;
                    chars = chars.Slice(1);
                    lineCount++;
                    continue;
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                }

                int nextIndex = 0;
                int indexOf = chars.IndexOfAny(searchValues);

                if (indexOf > -1)
                {
                    wasSpace = true;
                    nextIndex = indexOf + 1;

                    if (chars[indexOf] is NEWLINE)
                    {
                        lineCount++;       
                    }
                }
                else
                {
                    if (wasSpace)
                    {
                        wordCount++;
                    }

                    wasSpace = false;
                    nextIndex = chars.Length;
                }

                chars = chars.Slice(nextIndex);
            }
        }

        ArrayPool<char>.Shared.Return(buffer);
        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }
}
