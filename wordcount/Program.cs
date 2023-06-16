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

// string path = args.Length is 0 ? CountMultiFile.DirectoryPath : args[0];
// var counts = CountMultiFile.Count_File_OpenHandle(path);
// CountMultiFile.PrintCounts(counts);

// string path = args.Length > 0 ? args[0] : CountOneFile.FilePath;
// var counts = CountOneFile.Count_File_OpenHandle(path);
// CountOneFile.PrintCounts(counts);

// var countGroup = new Counts[]
// {
//     CountOneFile.Count_File_ReadAllLines(path),
//     CountOneFile.Count_File_ReadLines(path),
//     CountOneFile.Count_File_OpenText(path),
//     CountOneFile.Count_File_Open(path),
//     CountOneFile.Count_File_OpenHandle(path),
// };

// foreach (var count in countGroup)
// {
//     Console.WriteLine($"{count.Line} {count.Word} {count.Bytes} {count.File}");
// }

public record struct Counts(int Line, int Word, int Bytes, string File);

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class CountOneFile
{ 
    private static readonly int Size = 16 * 1024;
    public static readonly string FilePath = "Clarissa_Harlowe/clarissa_volume1.txt";

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
}

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class CountMultiFile
{
    public static readonly string DirectoryPath = "./Clarissa_Harlowe/";

    [Benchmark]
    public List<Counts> File_ReadLines() => Count_File_ReadLines(CountMultiFile.DirectoryPath);

    public static List<Counts> Count_File_ReadLines(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new Exception($"Path doesn't exist: {path}; Current directory: {Directory.GetCurrentDirectory()}");
        }

        List<Counts> results = new();

        foreach (var file in Directory.EnumerateFiles(path).OrderBy(f => f))
        {
            var counts = CountOneFile.Count_File_ReadLines(file);
            results.Add(counts);
        }

        return results;
    }

    [Benchmark(Baseline = true)]
    public List<Counts> File_OpenHandle() => Count_File_OpenHandle(CountMultiFile.DirectoryPath);

    public static List<Counts> Count_File_OpenHandle(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new Exception($"Path doesn't exist: {path}; Current directory: {Directory.GetCurrentDirectory()}");
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
