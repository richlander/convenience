using System.Buffers;
using System.Text;
using BenchmarkData;

namespace FileOpenRuneBenchmark;

public static class FileOpenRuneBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;
        int index = 0;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);

        int count = 0;
        while ((count = stream.Read(buffer.AsSpan(index))) > 0 || index > 0)
        {                
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count + index);
            index = 0;

            while (bytes.Length > 0)
            {
                var status = Rune.DecodeFromUtf8(bytes, out Rune rune, out int bytesConsumed);

                // bad read due to low buffer length
                if (status == OperationStatus.NeedMoreData && count > 0)
                {
                    bytes[..bytesConsumed].CopyTo(buffer); // move the partial Rune to the start of the buffer before next read
                    index = bytesConsumed;
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
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
