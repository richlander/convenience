using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BenchmarkData;

public static class BenchmarkValues
{
    public static int Size => 4 * 1024;

    public static char[] WhitespaceValues = [ (char)9, (char)10, (char)11, (char)12, (char)13, (char)32, (char)133, (char)160, (char)5760, (char)8192, (char)8193, (char)8194, (char)8195, (char)8196, (char)8197, (char)8198, (char)8199, (char)8200, (char)8201, (char)8202, (char)8232, (char)8233, (char)8239, (char)8287, (char)12288];

    public static SearchValues<char> WhitespaceSearchValues = SearchValues.Create(WhitespaceValues);

}

public record struct Count(long Lines, long Words, long Bytes, string File);
