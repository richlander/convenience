using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
#pragma warning disable CA1050
#pragma warning disable CA1822
#pragma warning disable IDE0057

BenchmarkRunner.Run(typeof(ReleaseValue));

// var values = new ReleaseValue();
// string[] results = new string[]
// {
//     values.File_ReadAllLines(),
//     values.File_ReadLines(),
//     values.File_OpenText(),
//     values.File_Open(),
//     values.File_OpenHandle()
// };

// foreach (var result in results)
// {
//     Console.WriteLine(result);
// }

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class ReleaseValue
{
    private const char QUOTE = '"';
    private const int ORDINAL = 1;
    private const string KEY = "PRETTY_NAME=";
    private  string FILE = "/etc/os-release";

    [Benchmark]
    public string File_ReadAllLines()
    {
        var lines = File.ReadAllLines(FILE);
        var line = lines[ORDINAL];

        if (line.StartsWith(KEY, StringComparison.Ordinal))
        {
            var value = line.Substring(KEY.Length);
            value = value.TrimStart(QUOTE);
            value = value.TrimEnd(QUOTE);
            return value;
        }

        return $"{KEY} was not found.";
    }

    [Benchmark]
    public string File_ReadLines()
    {
        foreach (var line in File.ReadLines(FILE))
        {
            if (line.StartsWith(KEY, StringComparison.Ordinal))
            {
                var value = line.Substring(KEY.Length);
                value = value.TrimStart(QUOTE);
                value = value.TrimEnd(QUOTE);
                return value;
            }
        }

        return $"{KEY} was not found.";
    }

    [Benchmark]
    public string File_OpenText()
    {
        using var stream = File.OpenText(FILE);

        string? line = null;
        while ((line = stream.ReadLine()) is not null)
        {
            if (line.StartsWith(KEY, StringComparison.Ordinal))
            {
                var value = line.Substring(KEY.Length);
                value = value.TrimStart(QUOTE);
                value = value.TrimEnd(QUOTE);
                return value;
            }
        }

        return $"{KEY} was not found.";
    }

    [Benchmark]
    public string File_Open()
    {
        ReadOnlySpan<byte> keyBytes = "PRETTY_NAME="u8;
        ReadOnlySpan<byte> key = keyBytes;
        byte newLine = (byte)'\n';
        StringBuilder builder = new();

        using var stream = File.Open(FILE, FileMode.Open, FileAccess.Read,FileShare.Read);
        int size = 1024;
        Span<byte> buffer = stackalloc byte[size];
        ReadOnlySpan<byte> text = buffer;

        int read = 0;
        bool doRead = true;
        bool readToNewLine = false;
        bool foundTerm = false;

        while (doRead && (read = stream.Read(buffer)) > 0)
        {
            text = buffer.Slice(0, read);

            while (text.Length > 0)
            {
                // Read until next newline
                if (readToNewLine)
                {
                    int indexOf = text.IndexOf(newLine);

                    // No newline char or valuable text
                    if (indexOf is -1
                        || indexOf >= text.Length)
                    {
                        // Re-fill buffer
                        break;
                    }

                    // Slice to just after newline
                    text = text.Slice(indexOf + 1);
                    readToNewLine = false;
                }
                else if (foundTerm)
                {
                    int newLineIndex = text.IndexOf(newLine);
                    if (text.Length > 0)
                    {
                        // Grab more content if newline was not found
                        if (newLineIndex is -1)
                        {
                            Append(text, builder);
                        }
                        else
                        {
                            // Remove past (and including) newline character
                            Append(text.Slice(0, newLineIndex), builder);
                            // Done
                            doRead = false;
                        }

                        // Re-fill buffer
                        break;
                    }
                }

                // Find a match
                if (key.Length <= text.Length
                    && text.Slice(0, key.Length).IndexOf(key) is 0)
                {
                    text = text.Slice(key.Length);
                    foundTerm = true;
                    continue;
                }
                // Find partial match
                else if (key.Length > text.Length
                        && key.Slice(0, text.Length).IndexOf(text) is 0)
                {
                    key = key.Slice(text.Length);
                    // Re-fill buffer
                    break;
                }
                // Reset term variable if needed (due to partial matching)
                else if (key.Length != KEY.Length)
                {
                    key = keyBytes;
                }
                
                readToNewLine = true;
            }
        }

        return builder.ToString();
    }

    [Benchmark(Baseline = true)]
    public string File_OpenHandle()
    {
        ReadOnlySpan<byte> keyBytes = "PRETTY_NAME="u8;
        ReadOnlySpan<byte> key = keyBytes;
        byte newLine =  (byte)'\n';
        StringBuilder builder = new();

        int size = 1024;
        Span<byte> buffer = stackalloc byte[size];
        ReadOnlySpan<byte> text = buffer;
        using var handle = File.OpenHandle(FILE, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        int read = 0;
        bool doRead = true;
        bool readToNewLine = false;
        bool foundTerm = false;
        int totalBytes = 0;

        while (doRead && (read = RandomAccess.Read(handle, buffer, totalBytes)) > 0)
        {
            // Scenario is to capture text to EOL (minus a trailing quote) after search term.
            // There are multiple cases to support.
            // Imagine search term "FOO=" 
            // "FOO=blah\n"
            // "FOO=blah"
            // "FOO=b"
            // "F"
            // "O"
            // Insights for format:
            // Search term is always at the start of newline.
            // If the term doesn't match, search for next newline.
            // A newline could appear before the search term.

            totalBytes += read;
            text = buffer.Slice(0, read);

            while (text.Length > 0)
            {
                // Read until next newline
                if (readToNewLine)
                {
                    int indexOf = text.IndexOf(newLine);

                    // No newline char or valuable text
                    if (indexOf is -1
                        || indexOf >= text.Length)
                    {
                        // Re-fill buffer
                        break;
                    }

                    // Slice to just after newline
                    text = text.Slice(indexOf + 1);
                    readToNewLine = false;
                }
                else if (foundTerm)
                {
                    int newLineIndex = text.IndexOf(newLine);
                    if (text.Length > 0)
                    {
                        // Grab more content if newline was not found
                        if (newLineIndex is -1)
                        {
                            Append(text, builder);
                        }
                        else
                        {
                            // Remove past (and including) newline character
                            Append(text.Slice(0, newLineIndex), builder);
                            // Done
                            doRead = false;
                        }

                        // Re-fill buffer
                        break;
                    }
                }

                // Find a match
                if (key.Length <= text.Length
                    && text.Slice(0, key.Length).IndexOf(key) is 0)
                {
                    text = text.Slice(key.Length);
                    foundTerm = true;
                    continue;
                }
                // Find partial match
                else if (key.Length > text.Length
                        && key.Slice(0, text.Length).IndexOf(text) is 0)
                {
                    key = key.Slice(text.Length);
                    // Re-fill buffer
                    break;
                }
                // Reset term variable if needed (due to partial matching)
                else if (key.Length != KEY.Length)
                {
                    key = keyBytes;
                }
                
                readToNewLine = true;
            }
        }

        return builder.ToString();
    }

    void Append(ReadOnlySpan<byte> value, StringBuilder builder)
    {
        if (value.Length is 0)
        {
            return;
        }

        if (value[0] is (byte)QUOTE)
        {
            if (value.Length is 1)
            {
                return;
            }

            value = value.Slice(1);
        }

        if (value[value.Length - 1] is (byte)QUOTE)
        {
            if (value.Length is 1)
            {
                return;
            }

            value = value.Slice(0, value.Length - 1);
        }

        string result = Encoding.ASCII.GetString(value);
        builder.Append(result);
    }
}