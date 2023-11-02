using System.Buffers;
using BenchmarkData;

namespace FileOpenTextCharBenchmark;

public static class FileOpenTextCharBenchmark
{
    public static Count Count(string path)
    {
        long wordCount = 0, lineCount = 0, charCount = 0;
        bool wasSpace = true;

        char[] buffer = ArrayPool<char>.Shared.Rent(BenchmarkValues.Size);
        using StreamReader reader = File.OpenText(path);

        int count = 0;
        while ((count = reader.Read(buffer)) > 0)
        {
            charCount += count;
            Span<char> chars = buffer.AsSpan(0, count);

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

        ArrayPool<char>.Shared.Return(buffer);
        return new(lineCount, wordCount, charCount, path);
    }
}
