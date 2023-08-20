using System.Text.Json.Serialization;
using ReleaseJson;

namespace ReportJson;

public record ReleaseReport([property: JsonPropertyName("report-date")] string ReportDate, [property: JsonPropertyName("versions")] IList<Version> Versions);

public record Version([property: JsonPropertyName("version")] string MajorVersion, [property: JsonPropertyName("supported")] bool Supported, [property: JsonPropertyName("eol-date")] string EolDate, [property: JsonPropertyName("support-ends-in-days")] int SupportEndsInDays, [property: JsonPropertyName("releases")] IList<Release> Releases);

public record Release([property: JsonPropertyName("release-version")] string BuildVersion, [property: JsonPropertyName("security")] bool Security, [property: JsonPropertyName("release-date")] string ReleaseDate, [property: JsonPropertyName("released-days-ago")] int ReleasedDaysAgo, [property: JsonPropertyName("cve-list")] IList<Cve> Cves);