using System.Buffers;
using BenchmarkData;

namespace FileOpenBenchmark;

public static class FileOpenBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);

        int count = 0;
        while ((count = stream.Read(buffer)) > 0)
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
