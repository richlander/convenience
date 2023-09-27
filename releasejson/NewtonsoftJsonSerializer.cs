using System.Diagnostics.CodeAnalysis;
using JsonConfig;
using Newtonsoft.Json;

namespace NewtonsoftJsonSerializerBenchmark;

public class NewtonsoftJsonSerializerBenchmark
{
    public static async Task<int> RunAsync()
    {
        var json = await MakeReportAsync(JsonBenchmark.Url);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<int> RunLocalAsync()
    {
        var json = MakeReportLocalAsync(JsonBenchmarkLocal.Path);
        Console.WriteLine(json);
        Console.WriteLine();
        // This is here to maintain the same signature as the other test methods
        // Because Json.NET doesn't have an async serializer
        await Task.CompletedTask;
        return json.Length;
    }

    public static async Task<string> MakeReportAsync(string url)
    {
        // Make network call
        using var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        using var stream = await releaseMessage.Content.ReadAsStreamAsync();

        // Attach stream to serializer
        JsonSerializer serializer = new();
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);

        // Process JSON
        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception();
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonConvert.SerializeObject(report);
    }

    public static string MakeReportLocalAsync(string file)
    {
        // Local local file
        using Stream stream = File.Open(file, FileMode.Open);

        // Attach stream to serializer
        JsonSerializer serializer = new();
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);

        // Process JSON
        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception();
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonConvert.SerializeObject(report);
    }

    public static Version GetVersion(MajorRelease release)
    {
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        bool supported = release.SupportPhase is "active" or "maintainence";
        Version version = new(release.ChannelVersion, supported, release.EolDate ?? "Unknown", supportDays, GetReleases(release).ToList());
        return version;
    }

  // Get first and first security release
    public static IEnumerable<Release> GetReleases(MajorRelease release)
    {
        bool securityOnly = false;
        
        foreach (ReleaseDetail releaseDetail in release.Releases)
        {
            if (securityOnly && !releaseDetail.Security)
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
public record MajorRelease(
    [property: JsonProperty("channel-version")] string ChannelVersion, 
    [property: JsonProperty("latest-release")] string LatestRelease, 
    [property: JsonProperty("latest-release-date")] string LatestReleaseDate, 
    [property: JsonProperty("security")] bool Security, 
    [property: JsonProperty("latest-runtime")] string LatestRuntime, 
    [property: JsonProperty("latest-sdk")] string LatestSdk, 
    [property: JsonProperty("release-type")] string ReleaseType, 
    [property: JsonProperty("support-phase")] string SupportPhase, 
    [property: JsonProperty("eol-date")] string EolDate, 
    [property: JsonProperty("releases.json")] string ReleasesJson, 
    [property: JsonProperty("releases")] List<ReleaseDetail> Releases
    );

public record ReleaseDetail(
    [property: JsonProperty("release-date")] string ReleaseDate, 
    [property: JsonProperty("release-version")] string ReleaseVersion, 
    [property: JsonProperty("security")] bool Security, 
    [property: JsonProperty("cve-list")] List<Cve> Cves
    );

public record Cve(
    [property: JsonProperty("cve-id")] string CveId,
    [property: JsonProperty("cve-url")] string CveUrl
    );

// Report
public record Report(
    [property: JsonProperty("report-date")] string ReportDate, 
    [property: JsonProperty("versions")] IList<Version> Versions
    );

public record Version(
    [property: JsonProperty("version")] string MajorVersion, 
    [property: JsonProperty("supported")] bool Supported, 
    [property: JsonProperty("eol-date")] string EolDate, 
    [property: JsonProperty("support-ends-in-days")] int SupportEndsInDays, 
    [property: JsonProperty("releases")] IList<Release> Releases
    );

public record Release(
    [property: JsonProperty("release-date")] string ReleaseDate, 
    [property: JsonProperty("released-days-ago")] int ReleasedDaysAgo, 
    [property: JsonProperty("release-version")] string BuildVersion, 
    [property: JsonProperty("security")] bool Security, 
    [property: JsonProperty("cve-list")] IList<Cve> Cves
    );
