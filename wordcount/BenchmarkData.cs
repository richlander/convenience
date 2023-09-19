namespace BenchmarkData;

public static class BenchmarkValues
{
    public static string FilePath => "Clarissa_Harlowe/clarissa_volume1.txt";
    
    public static int Size => 16 * 1024;
}

public record Benchmark(string Name, Func<string, Counts> Test);

public record struct BenchmarkResult(int Pass, string Benchmark, long Duration, Counts Counts);

public record struct Counts(int Lines, int Words, int Bytes, string File);
