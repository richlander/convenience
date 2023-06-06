// A word is a non-zero-length sequence of characters delimited by white space.
// "The code"
// For space between "The" and "code"

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

// var summary = BenchmarkRunner.Run(typeof(Foo));

var f = new Foo();
var counts = new Counts[]
{
    f.File_ReadLines(),
    f.File_OpenText(),
    f.File_Open(),
    f.File_OpenHandle_RandomAccess(),
};

foreach (var count in counts)
{
    Console.WriteLine($"{count.Line} {count.Word} {count.Character}");
}

// var count = f.File_OpenHandle_RandomAccess_IndexOf();
// Console.WriteLine($"{count.Line} {count.Word} {count.Character}");

public record struct Counts(int Line, int Word, int Character);

[MemoryDiagnoser]
public class Foo
{ 
    string FilePath = "text.txt";
    int Size = 16 * 1024;

    [Benchmark(Baseline = true)]
    public Counts File_ReadLines()
    {
        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;

        foreach (var line in File.ReadLines(FilePath))
        {
            lineCount++;
            charCount += line.Length;
            bool wasSpace = true;

            foreach (var c in line)
            {
                bool isSpace = Char.IsWhiteSpace(c);

                if (!isSpace && wasSpace)
                {
                    wordCount++;
                }

                wasSpace = isSpace;
            }
        }

        return new(lineCount, wordCount, charCount);
    }

    [Benchmark]
    public Counts File_OpenText()
    {
        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;
        using var stream = File.OpenText(FilePath);

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            lineCount++;
            charCount += line.Length;
            bool wasSpace = true;

            foreach (var c in line)
            {
                bool isSpace = Char.IsWhiteSpace(c);

                if (!isSpace && wasSpace)
                {
                    wordCount++;
                }

                wasSpace = isSpace;
            }
        }

        return new(lineCount, wordCount, charCount);
    }

    [Benchmark]
    public Counts File_Open()
    {
        const byte NEWLINE = (byte)'\n';
        const byte SPACE = (byte)' ';
        ReadOnlySpan<byte> searchChars = stackalloc[] {SPACE, NEWLINE};

        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;

        using var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(Size);
        Span<byte> buffer = rentedArray;
        ReadOnlySpan<byte> text = buffer;

        bool wasSpace = true;

        int read = 0;
        while ((read = stream.Read(buffer)) > 0)
        {                
            text = buffer.Slice(0, read);
            
            while (text.Length > 0)
            {
                bool isSpace = char.IsWhiteSpace((char)text[0]);
                int indexOf = text.IndexOfAny(searchChars);
                int nextIndex = 0;

                if (wasSpace && !isSpace)
                {
                    wordCount++;
                }

                wasSpace = isSpace;

                if (text[0] is SPACE)
                {
                    nextIndex = 1;
                }
                else if (text[0] is NEWLINE)
                {
                    nextIndex = 1;
                    wasSpace = true;
                    lineCount++;
                }
                else if (indexOf > -1 && text[indexOf] is SPACE)
                {
                    nextIndex = indexOf + 1;
                    wasSpace = true;
                }
                else if (indexOf > -1)
                {
                    nextIndex = indexOf + 1;
                    lineCount++;                   
                    wasSpace = true;
                }
                else
                {
                    nextIndex = text.Length;
                }

                text = text.Slice(nextIndex);
                charCount += nextIndex;
            }
        }

        ArrayPool<byte>.Shared.Return(rentedArray);
        return new(lineCount, wordCount, charCount);
    }

    [Benchmark]
    public Counts File_OpenHandle_RandomAccess()
    {
        const byte NEWLINE = (byte)'\n';
        const byte SPACE = (byte)' ';
        ReadOnlySpan<byte> searchChars = stackalloc[] {SPACE, NEWLINE};

        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;

        using var handle = File.OpenHandle(FilePath);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(Size);
        Span<byte> buffer = rentedArray;
        ReadOnlySpan<byte> text = buffer;

        int totalBytes = 0;
        int read = 0;
        bool wasSpace = true;

        while ((read = RandomAccess.Read(handle, buffer, totalBytes)) > 0)
        {
            totalBytes += read;

            text = buffer.Slice(0, read);
            
            while (text.Length > 0)
            {
                bool isSpace = char.IsWhiteSpace((char)text[0]);
                int indexOf = text.IndexOfAny(searchChars);
                int nextIndex = 0;

                if (wasSpace && !isSpace)
                {
                    wordCount++;
                }

                wasSpace = isSpace;

                if (text[0] is SPACE)
                {
                    nextIndex = 1;
                }
                else if (text[0] is NEWLINE)
                {
                    nextIndex = 1;
                    wasSpace = true;
                    lineCount++;
                }
                else if (indexOf > -1 && text[indexOf] is SPACE)
                {
                    nextIndex = indexOf + 1;
                    wasSpace = true;
                }
                else if (indexOf > -1)
                {
                    nextIndex = indexOf + 1;
                    lineCount++;                   
                    wasSpace = true;
                }
                else
                {
                    nextIndex = text.Length;
                }

                text = text.Slice(nextIndex);
                charCount += nextIndex;
            }
        }

        ArrayPool<byte>.Shared.Return(rentedArray);
        return new(lineCount, wordCount, charCount);
    }
}
