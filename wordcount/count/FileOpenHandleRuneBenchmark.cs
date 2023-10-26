using System.Buffers;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkData;

namespace FileOpenHandleRuneBenchmark;

public static class FileOpenHandleRuneBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        int index = 0;

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer.AsSpan(index), byteCount)) > 0 || index > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count + index);

            while (bytes.Length > 0)
            {
                var status = Rune.DecodeFromUtf8(bytes, out Rune rune, out int bytesConsumed);

                // bad read due to low buffer length
                if (status is not OperationStatus.Done && bytes.Length < 4)
                {
                    break;
                }
                
                if (Rune.IsWhiteSpace(rune))
                {
                    if (bytes[0] is (byte)'\n')
                    {
                        lineCount++;
                    }

                    wasSpace = true;
                }
                else if (wasSpace)
                {
                    wordCount++;
                    wasSpace = false;
                }

                bytes = bytes.Slice(bytesConsumed);
            }

            index = bytes.Length;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}