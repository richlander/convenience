namespace JsonConfig;

public static class JsonBenchmarkLocal
{
    public const string Official = "releases.json";
    public const string FakeSecurityReleaseNearEnd ="fake-release-near-end.json";
    public const string FakeSecurityReleaseAtEnd = "fake-release-at-end.json";
    public const string FakeNoSecurityRelease = "fake-no-security-release.json";
    public const string FakeMetadataOnly = "fake-metadata-only.json";
    public const string FakeOneReleaseOnly = "fake-one-release-only.json";
    public static string File => Official;

    public static string LocalHost = "http://localhost:5255/";

    public static string GetFile() => Path.Combine(Path.GetDirectoryName(typeof(JsonBenchmarkLocal).Assembly.Location) ?? throw new Exception("Directory not found"), "releasejson", File);

    public static string GetUrl() => $"{LocalHost}{File}";
}