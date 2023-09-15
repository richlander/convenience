namespace JsonConfig;

public static class JsonBenchmark
{
    public const string Official = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/6.0/releases.json";
    public const string FakeSecurityReleaseNearEnd = "https://gist.githubusercontent.com/richlander/408cd63769ab12386729e926f25d8858/raw/064d23ace456176f67784ed80aa5ce2d5b15f333/releases.json";
    public const string FakeSecurityReleaseAtEnd = "https://gist.githubusercontent.com/richlander/408cd63769ab12386729e926f25d8858/raw/064d23ace456176f67784ed80aa5ce2d5b15f333/releases.json";
    public const string FakeNoSecurityRelease = "https://gist.githubusercontent.com/richlander/e7f1d03e0cea76539325dcc07a8f48df/raw/e05494f2c7e10715dfcd0cdcc2dc1fbd7cb89219/releases.json";
    public const string FakeSmallOneReleaseOnly = "https://gist.githubusercontent.com/richlander/f965ba65696efb6187c727c0e3e9f7dc/raw/277eee6c6b545e6cc66a50061f5f2ad2170ae69c/releases.json";
    public const string FakeMediumReleaseMetadataOnly = "https://gist.githubusercontent.com/richlander/ce8e5fd8c29a8722f6cb9d3d3bb7fb55/raw/2dc0d16a6272851c59ba3db6e23779b6518b22a1/releases.json";
    public const string BADJSON = "JSON data is wrong";
    public const string BADJSONREAD = "Cannot read JSON data.";
    public const string JSONOUTOFORDER = "JSON is bring read out of order";

    public static string Url => Official;
}