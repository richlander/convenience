using System.Buffers;
using System.Diagnostics;
using BenchmarkData;

namespace FileOpenHandleBenchmark;

public static class FileOpenHandleBenchmark
{
    public static Count Count(string path)
    {
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
                char c = (char)bytes[0];

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

                bytes = bytes.Slice(1);
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
