// A word is a non-zero-length sequence of characters delimited by white space.
// "The code"
// For space between "The" and "code"

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis.Text;

BenchmarkRunner.Run(typeof(Foo));

// var f = new Foo();
// var counts = new Counts[]
// {
//     f.File_ReadLines(),
//     f.File_OpenText(),
//     f.File_Open(),
//     f.File_OpenHandle(),
//     f.File_OpenHandle_Range(),
// };

// foreach (var count in counts)
// {
//     Console.WriteLine($"{count.Line} {count.Word} {count.Character}");
// }

// var count = f.File_OpenHandle();
// Console.WriteLine($"{count.Line} {count.Word} {count.Character}");

public record struct Counts(int Line, int Word, int Character);

[MemoryDiagnoser]
public class Foo
{ 
    string FilePath = "text.txt";
    int Size = 16 * 1024;

    // [Benchmark]s
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

    // [Benchmark]
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
        int byteCount = 0;
        int read = 0;
        bool wasSpace = true;

        using var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(Size);
        Span<byte> buffer = rentedArray;
        ReadOnlySpan<byte> text = buffer;

        while ((read = stream.Read(buffer)) > 0)
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
        return new(lineCount, wordCount, byteCount);
    }

    [Benchmark]
    public Counts File_OpenHandle()
    {
        const byte NEWLINE = (byte)'\n';
        const byte SPACE = (byte)' ';
        ReadOnlySpan<byte> searchChars = stackalloc[] {SPACE, NEWLINE};

        int wordCount = 0;
        int lineCount = 0;
        int byteCount = 0;
        int read = 0;
        bool wasSpace = true;

        using var handle = File.OpenHandle(FilePath);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(Size);
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
        return new(lineCount, wordCount, byteCount);
    }

    [Benchmark]
    public Counts File_OpenHandle_Range()
    {
        const byte NEWLINE = (byte)'\n';
        const byte SPACE = (byte)' ';
        ReadOnlySpan<byte> searchChars = stackalloc[] {SPACE, NEWLINE};

        int wordCount = 0;
        int lineCount = 0;
        int byteCount = 0;
        int read = 0;
        bool wasSpace = true;

        using var handle = File.OpenHandle(FilePath);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(Size);
        Span<byte> buffer = rentedArray;
        ReadOnlySpan<byte> text = buffer;

        while ((read = RandomAccess.Read(handle, buffer, byteCount)) > 0)
        {
            byteCount += read;
            text = buffer[0..read];
            
            while (text.Length > 0)
            {
                if (text[0] is SPACE)
                {
                    wasSpace = true;
                    text = text[1..];
                    continue;
                }
                else if (text[0] is NEWLINE)
                {
                    wasSpace = true;
                    text = text[1..];
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

                text = text[nextIndex..];
            }
        }

        ArrayPool<byte>.Shared.Return(rentedArray);
        return new(lineCount, wordCount, byteCount);
    }
}
