namespace JsonConfig;

public static class BenchmarkData
{
    // Error messages
    public const string BADJSON = "JSON data is wrong";
    public const string BADJSONREAD = "Cannot read JSON data.";
    public const string JSONOUTOFORDER = "JSON is bring read out of order";

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
    public const string RemoteHost = "https://raw.githubusercontent.com/richlander/convenience/json/releasejson/fakejson/";

    public static string File { get; set; } = FakeReleaseJson[0];

    public static string RemoteUrl => $"{RemoteHost}{File}";
    
    public static string LocalUrl => $"{LocalHost}{File}";
    
    public static string Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BenchmarkData).Assembly.Location) ?? throw new Exception("Directory not found"), File);
}