using System.Buffers;
using System.Text;
using BenchmarkData;

namespace FileOpenCharSearchValuesBenchmark;

public static class FileOpenCharSearchValuesBenchmark
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
        using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);

        int count = 0;
        while ((count = stream.Read(buffer)) > 0)
        {                
            byteCount += count;
            int charCount = decoder.GetChars(buffer.AsSpan(0, count), charBuffer, false);
            ReadOnlySpan<char> chars = charBuffer.AsSpan(0, charCount);

            while (chars.Length > 0)
            {
                if (char.IsWhiteSpace(chars[0]))
                {
                    if (chars[0] is '\n')
                    {
                        lineCount++;                      
                    }

                    wasSpace = true;
                    chars = chars.Slice(1);
                    continue;
                }
                else if (wasSpace)
                {
                    wordCount++;
                    wasSpace = false;
                    chars = chars.Slice(1);
                }

                int index = chars.IndexOfAny(BenchmarkValues.WhitespaceSearchValues);

                if (index > -1)
                {
                    if (chars[index] is '\n')
                    {
                        lineCount++;       
                    }

                    wasSpace = true;
                    chars = chars.Slice(index + 1);
                }
                else
                {
                    wasSpace = false;
                    chars = [];
                }
            }
        }

        ArrayPool<char>.Shared.Return(charBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, path);
    }
}
