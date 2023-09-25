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
        ReadOnlySpan<byte> searchChars = stackalloc[] {SPACE, NEWLINE};

        int wordCount = 0;
        int lineCount = 0;
        int byteCount = 0;
        int read = 0;
        bool wasSpace = true;

        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        Span<byte> buffer = rentedArray;
        ReadOnlySpan<byte> text = buffer;

        while ((read = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += read;
            text = buffer.Slice(0, read);
            
            while (text.Length > 0)
            {
                if (text[0] is SPACE)
                {
                    wasSpace = true;
                    text = text.Slice(1);
                    continue;
                }
                else if (text[0] is CARRIAGE_RETURN)
                {
                    text = text.Slice(1);
                    continue;
                }
                else if (text[0] is NEWLINE)
                {
                    wasSpace = true;
                    text = text.Slice(1);
                    lineCount++;
                    continue;
                }
                else if (wasSpace)
                {
                    wasSpace = false;
                    wordCount++;
                }

                int nextIndex = 0;
                int indexOf = text.IndexOfAny(searchChars);

                if (indexOf > -1)
                {
                    wasSpace = true;
                    nextIndex = indexOf + 1;

                    if (text[indexOf] is NEWLINE)
                    {
                        lineCount++;       
                    }
                }
                else
                {
                    if (wasSpace)
                    {
                        wordCount++;
                    }

                    wasSpace = false;
                    nextIndex = text.Length;
                }

                text = text.Slice(nextIndex);
            }
        }

        ArrayPool<byte>.Shared.Return(rentedArray);
        return new(lineCount, wordCount, byteCount, Path.GetFileName(path));
    }
}
