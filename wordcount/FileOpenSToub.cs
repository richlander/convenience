

using System.Buffers;
using BenchmarkData;

namespace FileOpenSToubBenchmark;

public static class FileOpenSToubBenchmark
{
    public static Counts Count(string path)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BenchmarkValues.Size);
        int wordCount = 0, lineCount = 0, byteCount = 0;
        bool inWord = false;

        using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        int count;
        while ((count = RandomAccess.Read(file, buffer, byteCount)) > 0)
        {
            byteCount += count;
            Span<byte> bytes = buffer.AsSpan(0, count);
            int pos;

            // If we're in a word, get out of it.
            if (inWord)
            {
                pos = bytes.IndexOfAny((byte)' ', (byte)'\n');
                if (pos < 0)
                {
                    continue;
                }

                bytes = bytes.Slice(pos);
                inWord = false;
            }

            // While there's still more data in our buffer, process it.
            while (!bytes.IsEmpty)
            {
                // Find the start of the next word
                if (bytes[0] is (byte)' ')
                {
                    bytes = bytes.Slice(1);
                    continue;
                }
                else if (bytes[0] is (byte)'\n')
                {
                    lineCount++;
                    bytes = bytes.Slice(1);
                    continue;
                }

                // We're at the start of a word. Count it and try to find its end.
                wordCount++;
                pos = bytes.IndexOfAny((byte)' ', (byte)'\n');
                if (pos < 0)
                {
                    inWord = true;
                    break;
                }

                if (bytes[pos] is (byte)'\n')
                {
                    lineCount++;
                }
                bytes = bytes.Slice(pos + 1);
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new(lineCount, wordCount, byteCount, Path.GetFileName(path));
    }
}