namespace JsonBenchmark;

public static class Benchmark
{
    // Fake releases.json documents
    // The first one is the official file as of August 2023 (for consistent numbers)
    // The others are more fake
    // Official source: https://github.com/dotnet/core/blob/main/release-notes/releases-index.json
    public static readonly string[] FakeReleaseJson = [
        "fake-one-release-only.json",
        "fake-releases.json",
        "fake-release-near-end.json",
        "fake-release-at-end.json",
        "fake-no-security-release.json",
        "fake-releases-compact.json",
    ];
    public const string LocalHost = "http://localhost:5255/";
    public const string RemoteHost = "https://raw.githubusercontent.com/richlander/convenience/main/releasejson/fakejson/";

    public static string File { get; set; } = FakeReleaseJson[0];

    public static string RemoteUrl => $"{RemoteHost}{File}";
    
    public static string LocalUrl => $"{LocalHost}{File}";
    
    public static string Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Benchmark).Assembly.Location) ?? throw new Exception("Directory not found"), File);

    public static List<BenchmarkInfo> WebBenchmarks =>
    [
        new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async Task<int> (url) => await JsonSerializerBenchmark.JsonSerializerBenchmark.MakeReportWebAsync(url), null),
        new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), async Task<int> (url) => await JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.MakeReportWebAsync(url), null),
        new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), async Task<int> (url) => await JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.MakeReportWebAsync(url), null),
        new(nameof(JsonNodeBenchmark.JsonNodeBenchmark), async Task<int> (url) => await JsonNodeBenchmark.JsonNodeBenchmark.MakeReportAsync(url), null),
        new(nameof(Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark), async Task<int> (url) => await Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark.MakeReportWebAsync(url), null),
        new(nameof(Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark), async Task<int> (url) => await Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark.MakeReportWebAsync(url), null),
        new(nameof(Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark), async Task<int> (url) => await Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark.MakeReportWebAsync(url), null),
        new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), async Task<int> (url) => await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.MakeReportWebAsync(url), null),
    ];

    public static List<BenchmarkInfo> FileBenchmarks =>
    [
        new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), async Task<int> (path) => await JsonSerializerBenchmark.JsonSerializerBenchmark.MakeReportFileAsync(path), null),
        new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), async Task<int> (path) => await JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.MakeReportFileAsync(path), null),
        new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), async Task<int> (path) => await JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.MakeReportFileAsync(path), null),
        new(nameof(JsonNodeBenchmark.JsonNodeBenchmark), async Task<int> (path) => await JsonNodeBenchmark.JsonNodeBenchmark.MakeReportFileAsync(path), null),
        new(nameof(Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark), async Task<int> (path) => await Utf8JsonReaderWriterStreamBenchmark.Utf8JsonReaderWriterStreamBenchmark.MakeReportFileAsync(path), null),
        new(nameof(Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark), async Task<int> (path) => await Utf8JsonReaderWriterPipelineBenchmark.Utf8JsonReaderWriterPipelineBenchmark.MakeReportFileAsync(path), null),
        new(nameof(Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark), async Task<int> (path) => await Utf8JsonReaderWriterStreamRawBenchmark.Utf8JsonReaderWriterStreamRawBenchmark.MakeReportFileAsync(path), null),
        new(nameof(NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark), null, (path) => NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.MakeReportFile(path)),
    ];

    public static List<BenchmarkInfo> MemoryBenchmarks =>
    [
        new(nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), null, (json) => JsonSerializerBenchmark.JsonSerializerBenchmark.MakeReportMemory(json)),
        new(nameof(JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark), null, (json) => JsonSerializerSourceGeneratorPocoBenchmark.JsonSerializerSourceGeneratorPocoBenchmark.MakeReportMemory(json)),
        new(nameof(JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark), null, (json) => JsonSerializerSourceGeneratorRecordBenchmark.JsonSerializerSourceGeneratorRecordBenchmark.MakeReportMemory(json)),
    ];
}

public record BenchmarkInfo(string Name, Func<string, Task<int>>? AsyncTest, Func<string, int>? Test);

public record BenchmarkResult(int Pass, string Name, long Duration, int Length);
