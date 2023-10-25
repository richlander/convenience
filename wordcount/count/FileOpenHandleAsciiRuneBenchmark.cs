using System.Buffers;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkData;

namespace FileOpenHandleAsciiRuneBenchmark;

public static class FileOpenHandleAsciiRuneBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        ReadOnlySpan<byte> searchValues = [9, 11, 10, 12, 13, 194, 225, 226, 227];
        int index = 0;

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer.AsSpan(index), byteCount)) > 0 || index > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count + index);

            while (bytes.Length > 0)
            {
                byte b = bytes[0];
                if (b < 128)
                {
                    if (b is (byte)' ')
                    {
                        wasSpace = true;
                    }
                    else if (searchValues.Contains(b))
                    {
                        if (b is (byte)'\n') // 10
                        {
                            wasSpace = true;
                            lineCount++;
                        }
                        else if (b is (byte)'\r') // 13
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
                    continue;
                }
                
                var status = Rune.DecodeFromUtf8(bytes, out Rune rune, out int bytesConsumed);

                if (status is not OperationStatus.Done && bytes.Length < 4)
                {
                    break;
                }
                
                if (Rune.IsWhiteSpace(rune))
                {
                    wasSpace = true;
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                }

                bytes = bytes.Slice(bytesConsumed);
            }

            index = bytes.Length;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
