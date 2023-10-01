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
    public const string FakeOfficial = "fake-releases.json";
    public const string FakeSecurityReleaseNearEnd ="fake-release-near-end.json";
    public const string FakeSecurityReleaseAtEnd = "fake-release-at-end.json";
    public const string FakeNoSecurityRelease = "fake-no-security-release.json";
    public const string FakeCompact = "fake-releases-compact.json";
    public const string FakeOneReleaseOnly = "fake-one-release-only.json";
    public const string LocalHost = "http://localhost:5255/";
    public const string RemoteHost = "https://raw.githubusercontent.com/richlander/convenience/json/releasejson/fakejson/";

    public static string File { get; set; } = FakeOfficial;

    public static string Url { get; set; } = RemoteUrl;

    public static string RemoteUrl => $"{RemoteHost}{File}";
    
    public static string LocalUrl => $"{LocalHost}{File}";
    
    public static string Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BenchmarkData).Assembly.Location) ?? throw new Exception("Directory not found"), File);
}