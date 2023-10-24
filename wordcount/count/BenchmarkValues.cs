using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BenchmarkData;

public static class BenchmarkValues
{
    public static int Size => 16 * 1024;

    public static SearchValues<char> WhitespaceSearch = SearchValues.Create(GetWhiteSpaceChars().AsSpan());

    public static WhiteSpaceValues GetWhiteSpaceChars()
    {
        WhiteSpaceValues whitespace = new();
        char c = Char.MinValue;
        int index = 0;

        while (c < char.MaxValue)
        {
            if (Char.IsWhiteSpace(c))
            {
                whitespace[index++] = c;
            }

            c++;
        }

        return whitespace;
    }
}

public record struct Count(int Lines, int Words, int Bytes, string File);

[InlineArray(Length)]
public struct WhiteSpaceValues
{
    private const int Length = 25;
    char _element;

    [UnscopedRef]
    public Span<char> AsSpan() => MemoryMarshal.CreateSpan<char>(ref _element, Length);
}
