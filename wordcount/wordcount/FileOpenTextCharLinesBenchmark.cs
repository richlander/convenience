using System.Buffers;
using BenchmarkData;

namespace FileOpenTextCharLinesBenchmark;

public static class FileOpenTextCharLinesBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, charCount = 0;
        bool wasSpace = true;

        char[] buffer = ArrayPool<char>.Shared.Rent(BenchmarkValues.Size);
        using StreamReader reader = File.OpenText(path);

        int index = 0;
        int count = 0;
        while ((count = reader.Read(buffer.AsSpan(index))) > 0)
        {
            charCount += count;
            Span<char> chars = buffer.AsSpan(0, count);
            index = 0;

            while (chars.Length > 0)
            {
                if (ReadNextLine(chars, out ReadOnlySpan<char> line))
                {
                    lineCount++;
                }
                else if (line.Length is 0)
                {
                    chars.CopyTo(buffer);
                    index = chars.Length;
                    break;
                }

                int len = line.Length;
                for (int i = 0; i < len; i++)
                {
                    bool isSpace = char.IsWhiteSpace(line[i]);

                    if (!isSpace && wasSpace)
                    {
                        wordCount++;
                    }

                    wasSpace = isSpace;
                }

                chars = chars.Slice(len);
            }
        }

        ArrayPool<char>.Shared.Return(buffer);
        return new(lineCount, wordCount, charCount, path);
    }

    static bool ReadNextLine(ReadOnlySpan<char> chars, out ReadOnlySpan<char> line)
    {
        int index = chars.IndexOfAny('\r','\n');

        // no break
        if (index is -1)
        {
            line = chars;
            return false;
        }
        // not long enough to peak ahead
        // return as if no characters seen
        else if (chars.Length < index + 2)
        {
            line = chars.Slice(0, index);
            return false;
        }
        else if (chars[index] is '\r' && chars[index + 1] is '\n')
        {
            index++;
        }

        // return line including break characters
        line = chars.Slice(0, index + 1);
        return true;;
    }
}
