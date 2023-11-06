using System.Buffers;
using BenchmarkData;

namespace FileOpenHandleAsciiCheatBenchmark;

public static class FileOpenHandleAsciiCheatBenchmark
{
    public static Count Count(string path)
    {
        const byte NEWLINE = (byte)'\n';
        const byte SPACE = (byte)' ';
        ReadOnlySpan<byte> searchValues = [SPACE, NEWLINE];

        long wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using Microsoft.Win32.SafeHandles.SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count);
            
            while (bytes.Length > 0)
            {
                // what's this character?
                if (bytes[0] <= SPACE)
                {
                    if (bytes[0] is NEWLINE)
                    {
                        lineCount++;
                    }

                    wasSpace = true;
                    bytes = bytes.Slice(1);
                    continue;
                }
                else if (wasSpace)
                {
                    wordCount++;
                }

                // Look ahead for next space or newline
                // this logic assumes that preceding char was non-whitespace
                int index = bytes.IndexOfAny(searchValues);

                if (index > -1)
                {
                    if (bytes[index] is NEWLINE)
                    {
                        lineCount++;
                    }

                    wasSpace = true;
                    bytes = bytes.Slice(index + 1);
                }
                else
                {
                    wasSpace = false;
                    bytes = [];
                }
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
