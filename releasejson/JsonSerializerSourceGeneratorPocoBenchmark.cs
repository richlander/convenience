using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonConfig;

namespace JsonSerializerSourceGeneratorPocoBenchmark;

public static class JsonSerializerSourceGeneratorPocoBenchmark
{
    public static async Task<int> RunAsync()
    {
        var json = await MakeReportAsync();
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<int> RunLocalAsync()
    {
        var json = await MakeReportLocalAsync();
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReportAsync()
    {
        using HttpClient httpClient= new();
        MajorRelease release = await httpClient.GetFromJsonAsync<MajorRelease>(JsonBenchmark.Url, ReleaseContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = Report.Get(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, ReportContext.Default.Report);
    }

    public static async Task<string> MakeReportLocalAsync()
    {
        using Stream stream = File.Open(JsonBenchmarkLocal.GetFile(),FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, ReleaseContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = Report.Get(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, ReportContext.Default.Report);
    }

    public static Version GetVersion(MajorRelease release)
    {
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        bool supported = release.SupportPhase is "active" or "maintainence";
        Version version = Version.Get(release.ChannelVersion ?? "", supported, release.EolDate ?? "Unknown", supportDays, GetReleases(release).ToList());
        return version;
    }

    // Get first and first security release
    public static IEnumerable<Release> GetReleases(MajorRelease release)
    {
        bool securityOnly = false;
        
        ArgumentNullException.ThrowIfNull(release.Releases);

        foreach (ReleaseDetail releaseDetail in release.Releases)
        {
            if (securityOnly && !releaseDetail.Security)
            {
                continue;
            }

            if (releaseDetail.ReleaseVersion is null ||
                releaseDetail.ReleaseDate is null ||
                releaseDetail.Cves is null)
            {
                throw new Exception(JsonBenchmark.BADJSON);
            }
            
            var reportRelease = Release.Get(releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.Cves);
            yield return reportRelease;

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

[JsonSerializable(typeof(MajorRelease))]
public partial class ReleaseContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(Report))]
public partial class ReportContext : JsonSerializerContext
{
}

public class MajorRelease
{
    [property: JsonPropertyName("channel-version")]
    public string? ChannelVersion { get; set; }

    [property: JsonPropertyName("latest-release")]
    public string? LatestRelease { get; set; }

    [property: JsonPropertyName("latest-release-date")]
    public string? LatestReleaseDate { get; set; }

    [property: JsonPropertyName("security")]
    public bool Security { get; set; }

    [property: JsonPropertyName("latest-runtime")]
    public string? LatestRuntime { get; set; }

    [property: JsonPropertyName("latest-sdk")]
    public string? LatestSdk { get; set; }

    [property: JsonPropertyName("release-type")]
    public string? ReleaseType { get; set; }

    [property: JsonPropertyName("support-phase")]
    public string? SupportPhase { get; set; }

    [property: JsonPropertyName("eol-date")]
    public string? EolDate { get; set; }

    [property: JsonPropertyName("releases.json")]
    public string? ReleasesJson { get; set; }

    [property: JsonPropertyName("releases")]
    public List<ReleaseDetail>? Releases { get; set; }
}

public class ReleaseDetail
{
    [property: JsonPropertyName("release-date")]
    public string? ReleaseDate { get; set; } 
    
    [property: JsonPropertyName("release-version")]
    public string? ReleaseVersion { get; set; } 
    
    [property: JsonPropertyName("security")]
    public bool Security { get; set; } 
    
    [property: JsonPropertyName("cve-list")]
    public List<Cve>? Cves { get; set; }
}

public class Cve
{
    [property: JsonPropertyName("cve-id")]
    public string? CveId { get; set; }

    [property: JsonPropertyName("cve-url")]
    public string? CveUrl { get; set; }
}

public class Report
{
    [property: JsonPropertyName("report-date")]
    public string? ReportDate { get; set; }

    [property: JsonPropertyName("versions")]
    public IList<Version>? Versions { get; set; }

    public static Report Get(string reportDate, IList<Version> versions) =>
        new()
        {
            ReportDate = reportDate,
            Versions = versions
        };
}

public class Version
{
    [property: JsonPropertyName("version")]
    public string? MajorVersion { get; set; }
    
    [property: JsonPropertyName("supported")]
    public bool Supported { get; set; }
    
    [property: JsonPropertyName("eol-date")]
    public string? EolDate { get; set; }
    
    [property: JsonPropertyName("support-ends-in-days")]
    public int SupportEndsInDays { get; set; }
    
    [property: JsonPropertyName("releases")]
    public IList<Release>? Releases { get; set; }

    public static Version Get(string majorVersion, bool supported, string eolDate, int SupportEndsInDays, IList<Release> releases) =>
        new()
        {
            MajorVersion = majorVersion,
            Supported = supported,
            EolDate = eolDate,
            SupportEndsInDays = SupportEndsInDays,
            Releases = releases
        };
}

public class Release
{
    [property: JsonPropertyName("release-version")]
    public string? BuildVersion { get; set; }
    
    [property: JsonPropertyName("security")]
    public bool Security { get; set; }
    
    [property: JsonPropertyName("release-date")]
    public string? ReleaseDate { get; set; }
    
    [property: JsonPropertyName("released-days-ago")]
    public int ReleasedDaysAgo { get; set; }
    
    [property: JsonPropertyName("cve-list")]
    public IList<Cve>? Cves { get; set; }

    public static Release Get(string releaseDate, int releasedDaysAgo, string buildVersion, bool security, IList<Cve> cves) => 
        new()
        {
            BuildVersion = buildVersion,
            Security = security,
            ReleaseDate = releaseDate,
            ReleasedDaysAgo = releasedDaysAgo,
            Cves = cves
        };
}
