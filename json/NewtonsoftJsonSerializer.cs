using Newtonsoft.Json;

namespace NewtonsoftJsonSerializerBenchmark;
public class NewtonsoftJsonSerializerBenchmark
{
    public static async Task<string> Run()
    {
        var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(FakeTestData.URL, HttpCompletionOption.ResponseHeadersRead);
        var stream = await releaseMessage.Content.ReadAsStreamAsync();
        JsonSerializer serializer = new();
        Release? release = null;
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);
        release = serializer.Deserialize<Release>(reader) ?? throw new Exception();
        
        
        var version = GetVersionForRelease(release);
        // var options = new JsonSerializerOptions(JsonSerializerOptions.Default);
        List<Version> versions= [version];
        Report report = new(DateTime.Today.ToShortDateString(), versions);
        var json = JsonConvert.SerializeObject(report);
        return json;
    }

    public static Version GetVersionForRelease(Release release)
    {
        List<ReportRelease> releases = [];
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        Version version = new(release.ChannelVersion, release.SupportPhase is "active" or "maintainence", release.EolDate ?? "Unknown", supportDays, releases);
        bool securityOnly = false;
        
        foreach (ReleaseDetail releaseDetail in release.Releases)
        {
            if (!releaseDetail.Security && securityOnly)
            {
                continue;
            }
            
            var reportRelease = new ReportRelease(releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.Cves);
            releases.Add(reportRelease);

            if (releaseDetail.Security)
            {
                break;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }
        }

        return version;

        static int GetDaysAgo(string date, bool positiveNumber = false)
        {
            bool success = DateTime.TryParse(date, out var day);
            var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
            return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
        }
    }   
}

// releases.json
public record Release([property: JsonProperty("channel-version")] string ChannelVersion, [property: JsonProperty("latest-release")] string LatestRelease, [property: JsonProperty("latest-release-date")] string LatestReleaseDate, [property: JsonProperty("security")] bool Security, [property: JsonProperty("latest-runtime")] string LatestRuntime, [property: JsonProperty("latest-sdk")] string LatestSdk, [property: JsonProperty("release-type")] string ReleaseType, [property: JsonProperty("support-phase")] string SupportPhase, [property: JsonProperty("eol-date")] string EolDate, [property: JsonProperty("releases.json")] string ReleasesJson, [property: JsonProperty("releases")] List<ReleaseDetail> Releases);

public record ReleaseDetail([property: JsonProperty("release-date")] string ReleaseDate, [property: JsonProperty("release-version")] string ReleaseVersion, [property: JsonProperty("security")] bool Security, [property: JsonProperty("cve-list")] List<Cve> Cves);

public record Cve([property: JsonProperty("cve-id")] string CveId,[property: JsonProperty("cve-url")] string CveUrl);

// Report
public record Report([property: JsonProperty("report-date")] string ReportDate, [property: JsonProperty("versions")] IList<Version> Versions);

public record Version([property: JsonProperty("version")] string MajorVersion, [property: JsonProperty("supported")] bool Supported, [property: JsonProperty("eol-date")] string EolDate, [property: JsonProperty("support-ends-in-days")] int SupportEndsInDays, [property: JsonProperty("releases")] IList<ReportRelease> Releases);

public record ReportRelease([property: JsonProperty("release-version")] string BuildVersion, [property: JsonProperty("security")] bool Security, [property: JsonProperty("release-date")] string ReleaseDate, [property: JsonProperty("released-days-ago")] int ReleasedDaysAgo, [property: JsonProperty("cve-list")] IList<Cve> Cves);