using System.Buffers;
using System.Text;
using BenchmarkData;

namespace FileOpenHandleRuneBenchmark;

public static class FileOpenHandleRuneBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using Microsoft.Win32.SafeHandles.SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        int index = 0;

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer.AsSpan(index), byteCount)) > 0 || index > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count + index);
            index = 0;

            while (bytes.Length > 0)
            {
                OperationStatus status = Rune.DecodeFromUtf8(bytes, out Rune rune, out int bytesConsumed);

                // bad read due to low buffer length
                if (status == OperationStatus.NeedMoreData && count > 0)
                {
                    bytes[..bytesConsumed].CopyTo(buffer); // move the partial Rune to the start of the buffer before next read
                    index = bytesConsumed;
                    break;
                }
                
                if (Rune.IsWhiteSpace(rune))
                {
                    if (rune.Value is '\n')
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
