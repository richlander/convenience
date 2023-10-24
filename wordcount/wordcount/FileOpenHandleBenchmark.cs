using System.Buffers;
using BenchmarkData;

namespace FileOpenHandleBenchmark;

public static class FileOpenHandleBenchmark
{
    public static Count Count(string path)
    {
        const byte NEWLINE = (byte)'\n';
        const byte CARRIAGE_RETURN = (byte)'\r';
        const byte SPACE = (byte)' ';

        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);

        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count);
            
            while (bytes.Length > 0)
            {
                if (bytes[0] is SPACE && char.IsAscii((char)bytes[0]))
                {
                    wasSpace = true;
                }
                else if (bytes[0] is NEWLINE && char.IsAscii((char)bytes[0]))
                {
                    wasSpace = true;
                    lineCount++;
                }
                else if (bytes[0] is CARRIAGE_RETURN && char.IsAscii((char)bytes[0]))
                {
                }
                // else if (char.IsWhiteSpace((char)bytes[0]))
                // {
                //     wasSpace = true;
                // }
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
