using JsonConfig;
using Newtonsoft.Json;

namespace NewtonsoftJsonSerializerBenchmark;

public class NewtonsoftJsonSerializerBenchmark
{
    public static async Task<int> Run()
    {
        var json = await MakeReport();
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReport()
    {
        var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(JsonBenchmark.Url, HttpCompletionOption.ResponseHeadersRead);
        var stream = await releaseMessage.Content.ReadAsStreamAsync();

        JsonSerializer serializer = new();
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);

        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception();
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        Version version = new(release.ChannelVersion, release.SupportPhase is "active" or "maintainence", release.EolDate ?? "Unknown", supportDays, GetReleasesForReport(release).ToList());
        Report report = new(DateTime.Today.ToShortDateString(), [version]);
        return JsonConvert.SerializeObject(report);
    }

  // Get first and first security release
    public static IEnumerable<Release> GetReleasesForReport(MajorRelease release)
    {
        bool securityOnly = false;
        
        foreach (ReleaseDetail releaseDetail in release.Releases)
        {
            if (!releaseDetail.Security && securityOnly)
            {
                continue;
            }
            
            yield return new Release(releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.Cves);

            if (releaseDetail.Security)
            {
                yield break;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }
        }

        yield break;
    }

    static int GetDaysAgo(string date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
}

// releases.json
public record MajorRelease([property: JsonProperty("channel-version")] string ChannelVersion, [property: JsonProperty("latest-release")] string LatestRelease, [property: JsonProperty("latest-release-date")] string LatestReleaseDate, [property: JsonProperty("security")] bool Security, [property: JsonProperty("latest-runtime")] string LatestRuntime, [property: JsonProperty("latest-sdk")] string LatestSdk, [property: JsonProperty("release-type")] string ReleaseType, [property: JsonProperty("support-phase")] string SupportPhase, [property: JsonProperty("eol-date")] string EolDate, [property: JsonProperty("releases.json")] string ReleasesJson, [property: JsonProperty("releases")] List<ReleaseDetail> Releases);

public record ReleaseDetail([property: JsonProperty("release-date")] string ReleaseDate, [property: JsonProperty("release-version")] string ReleaseVersion, [property: JsonProperty("security")] bool Security, [property: JsonProperty("cve-list")] List<Cve> Cves);

public record Cve([property: JsonProperty("cve-id")] string CveId,[property: JsonProperty("cve-url")] string CveUrl);

// Report
public record Report([property: JsonProperty("report-date")] string ReportDate, [property: JsonProperty("versions")] IList<Version> Versions);

public record Version([property: JsonProperty("version")] string MajorVersion, [property: JsonProperty("supported")] bool Supported, [property: JsonProperty("eol-date")] string EolDate, [property: JsonProperty("support-ends-in-days")] int SupportEndsInDays, [property: JsonProperty("releases")] IList<Release> Releases);

public record Release([property: JsonProperty("release-date")] string ReleaseDate, [property: JsonProperty("released-days-ago")] int ReleasedDaysAgo, [property: JsonProperty("release-version")] string BuildVersion, [property: JsonProperty("security")] bool Security, [property: JsonProperty("cve-list")] IList<Cve> Cves);
