using System.Buffers;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkData;

namespace FileOpenHandleMultiByteBenchmark;

public static class FileOpenHandleMultiByteBenchmark
{
    public static Count Count(string path)
    {
        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        ReadOnlySpan<byte> searchValues = [9, 11, 10, 12, 13, 194, 225, 226, 227];

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count);
            
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
                
                char c = (char)b;
                if (bytes.Length > 2)
                {
                    var chars = Encoding.UTF8.GetChars(bytes.Slice(0, 3).ToArray());
                    c = chars[0];
                }

                if (char.IsWhiteSpace(c))
                {
                    wasSpace = true;
                    bytes = bytes.Slice(3);
                    continue;
                }
                
                if (wasSpace)
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
