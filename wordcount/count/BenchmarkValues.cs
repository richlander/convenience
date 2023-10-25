using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BenchmarkData;

public static class BenchmarkValues
{
    public static int Size => 16 * 1024;

    public static SearchValues<byte> WhitespaceSearchAscii = SearchValues.Create((ReadOnlySpan<byte>)[9, 10, 11, 12, 13, 32, 194, 225, 226, 227]);

    public static SearchValues<char> WhitespaceSearch = SearchValues.Create(GetWhiteSpaceChars().ToArray());

    public static IEnumerable<char> GetWhiteSpaceChars()
    {
        char c = Char.MinValue;

        while (c < char.MaxValue)
        {
            if (Char.IsWhiteSpace(c))
            {
                yield return c;
            }

            c++;
        }
    }
}

public record struct Count(int Lines, int Words, int Bytes, string File);
