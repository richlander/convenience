namespace JsonConfig;

public static class JsonBenchmark
{
    public const string OfficialUrl = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/7.0/releases.json";
    public const string FakeUrl = "https://gist.githubusercontent.com/richlander/f00f5ce27c26acf9c6e0988219a8dbed/raw/7829be33cc6dd1b9fa9a8e5813a24241d43ae573/release.json";
    public const string FakeUrlSmall = "https://gist.githubusercontent.com/richlander/7945b779c6ccc13af791bb824c096a5a/raw/e9108b0edb281df39fa853b78b0c6e2d2a22b835/releases.json";
    public const string BADJSON = "JSON data is wrong";
    public const string BADJSONREAD = "Cannot read JSON data.";
    public const string JSONOUTOFORDER = "JSON is bring read out of order";

    public static string Url => FakeUrl;
}