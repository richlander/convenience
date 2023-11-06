using System.Buffers;
using System.Text;
using BenchmarkData;

namespace FileOpenHandleCharBenchmark;

public static class FileOpenHandleCharBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, byteCount = 0;
        bool wasSpace = true;

        Encoding encoding = Encoding.UTF8;
        Decoder decoder = encoding.GetDecoder();
        int charBufferSize = encoding.GetMaxCharCount(BenchmarkValues.Size);

        char[] charBuffer = ArrayPool<char>.Shared.Rent(charBufferSize);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        using Microsoft.Win32.SafeHandles.SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        // Read content in chunks, in buffer, at count lenght, starting at byteCount
        int count = 0;
        while ((count = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += count;
            int charCount = decoder.GetChars(buffer.AsSpan(0, count), charBuffer, false);
            ReadOnlySpan<char> chars = charBuffer.AsSpan(0, charCount);

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsWhiteSpace(c))
                {
                    if (c is '\n')
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
            }
        }

        ArrayPool<char>.Shared.Return(charBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
