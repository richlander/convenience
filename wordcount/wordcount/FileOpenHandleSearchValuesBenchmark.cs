using System.Buffers;
using System.Linq.Expressions;
using BenchmarkData;

namespace FileOpenHandleSearchValuesBenchmark;

public static class FileOpenHandleSearchValuesBenchmark
{
    // private static readonly SearchValues<byte> s_searchValues = SearchValues.Create(BenchmarkData.BenchmarkValues.Whitespace);

    public static Count Count(string path)
    {
        const byte NEWLINE = (byte)'\n';
        const byte CARRIAGE_RETURN = (byte)'\r';
        // const byte SPACE = (byte)' ';
        var searchValues = BenchmarkData.BenchmarkValues.AsciiWhitespaceSearch;

        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count);
            
            while (bytes.Length > 0)
            {
                if (bytes[0] is CARRIAGE_RETURN)
                {
                    bytes = bytes.Slice(1);
                    continue;
                }
                else if (bytes[0] is NEWLINE)
                {
                    wasSpace = true;
                    bytes = bytes.Slice(1);
                    lineCount++;
                    continue;
                }
                else if (Char.IsWhiteSpace((char)bytes[0]))
                {
                    wasSpace = true;
                    bytes = bytes.Slice(1);
                    continue;
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                }

                int nextIndex = 0;
                int indexOf = bytes.IndexOfAny(searchValues);

                if (indexOf > -1)
                {
                    wasSpace = true;
                    nextIndex = indexOf + 1;

                    if (bytes[indexOf] is NEWLINE)
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
                    nextIndex = bytes.Length;
                }

                bytes = bytes.Slice(nextIndex);
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
