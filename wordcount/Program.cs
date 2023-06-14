using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis.Text;

// https://github.com/dotnet/roslyn/issues/47629
#pragma warning disable IDE0057
#pragma warning disable CA1050
#pragma warning disable CA1822

BenchmarkRunner.Run(typeof(CountOneFile));
// BenchmarkRunner.Run(typeof(CountMultiFile));

// string path = args[0] ?? CountMultiFile.DirectoryPath;
// var counts = CountMultiFile.Count_File_OpenHandle(path);
// CountMultiFile.PrintCounts(counts);

// string path = args[0] ?? CountOneFile.FilePath;
// var counts = CountOneFile.Count_File_OpenHandle(path);
// CountOneFile.PrintCounts(counts);

// var f = new CountOneFile();
// var counts = new Counts[]
// {
//     f.File_ReadLines(),
//     f.File_OpenText(),
//     f.File_Open(),
//     f.File_OpenHandle(),
// };

// foreach (var count in counts)
// {
//     Console.WriteLine($"{count.Line} {count.Word} {count.Character} {count.File}");
// }

public record struct Counts(int Line, int Word, int Bytes, string File);

[MemoryDiagnoser]
public class CountOneFile
{ 
    private static readonly int Size = 16 * 1024;
    public static readonly string FilePath = "clarissa_volume1.txt";

    [Benchmark]
    public Counts File_ReadAllLines() => Count_File_ReadAllLines(FilePath);

    public static Counts Count_File_ReadAllLines(string path)
    {
        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;

        foreach (var line in File.ReadAllLines(path))
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

        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }

    [Benchmark]
    public Counts File_ReadLines() => Count_File_ReadLines(FilePath);

    public static Counts Count_File_ReadLines(string path)
    {
        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;

        foreach (var line in File.ReadLines(path))
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

        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }

    [Benchmark]
    public Counts File_OpenText() => Count_File_OpenText(FilePath);

    public static Counts Count_File_OpenText(string path)
    {
        int wordCount = 0;
        int lineCount = 0;
        int charCount = 0;
        using var stream = File.OpenText(path);

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

        return new(lineCount, wordCount, charCount, Path.GetFileName(path));
    }

    [Benchmark]
    public Counts File_Open() => Count_File_Open(FilePath);
    
    public static Counts Count_File_Open(string path)
    {
        const byte LINE_FEED = (byte)'\n';
        const byte CARRIAGE_RETURN = (byte)'\r';
        const byte SPACE = (byte)' ';
        ReadOnlySpan<byte> searchChars = stackalloc[] {SPACE, LINE_FEED};

        int wordCount = 0;
        int lineCount = 0;
        int byteCount = 0;
        int read = 0;
        bool wasSpace = true;

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
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
                else if (text[0] is CARRIAGE_RETURN)
                {
                    text = text.Slice(1);
                    continue;
                }
                else if (text[0] is LINE_FEED)
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

                    if (text[indexOf] is LINE_FEED)
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

    [Benchmark(Baseline = true)]
    public Counts File_OpenHandle() => Count_File_OpenHandle(FilePath);

    public static Counts Count_File_OpenHandle(string path)
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

    public static void PrintCounts(Counts counts)
    {
        Console.WriteLine($"{counts.Line} {counts.Word} {counts.Bytes} {counts.File}");
    }

    public static void PrintMultipleCounts(IEnumerable<Counts> countsSet)
    {
        foreach (var counts in countsSet)
        {
            PrintCounts(counts);
        }
    }

    [Benchmark]
    public Counts File_Open2()
    {
        return Count_File_Open2(FilePath);
    }

    public static Counts Count_File_Open2(string path)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(Size);
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

[MemoryDiagnoser]
public class CountMultiFile
{
    public static readonly string DirectoryPath = "/home/rich/git/convenience/Clarissa_Harlowe/";

    [Benchmark]
    public List<Counts> File_ReadLines() => Count_File_ReadLines(CountMultiFile.DirectoryPath);

    public static List<Counts> Count_File_ReadLines(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new Exception();
        }

        List<Counts> results = new();

        foreach (var file in Directory.EnumerateFiles(path).OrderBy(f => f))
        {
            var counts = CountOneFile.Count_File_ReadLines(file);
            results.Add(counts);
        }

        return results;
    }

    [Benchmark]
    public List<Counts> File_OpenHandle() => Count_File_OpenHandle(CountMultiFile.DirectoryPath);

    public static List<Counts> Count_File_OpenHandle(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new Exception();
        }

        List<Counts> results = new();

        foreach (var file in Directory.EnumerateFiles(path).OrderBy(f => f))
        {
            var counts = CountOneFile.Count_File_OpenHandle(file);
            results.Add(counts);
        }

        return results;
    }

    public static void PrintCounts(List<Counts> counts)
    {
        foreach (var count in counts)
        {
            Console.WriteLine($"{count.Line} {count.Word} {count.Bytes} {count.File}");
        }

        var totalLines = counts.Sum(c => c.Line);
        var totalWords = counts.Sum(c => c.Word);
        var totalBytes = counts.Sum(c => c.Bytes);

        Console.WriteLine($"{totalLines} {totalWords} {totalBytes} total");
    }
}
