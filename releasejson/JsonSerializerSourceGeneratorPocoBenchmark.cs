using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonConfig;

namespace JsonSerializerSourceGeneratorPocoBenchmark;

public static class JsonSerializerSourceGeneratorPocoBenchmark
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
        var json = await MakeReportLocalAsync(JsonBenchmark.Path);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReportAsync(string url)
    {
        using HttpClient httpClient= new();
        MajorRelease release = await httpClient.GetFromJsonAsync<MajorRelease>(url, ReleaseContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = new()
        {
            ReportDate = DateTime.Today.ToShortDateString(), 
            Versions = [ GetVersion(release) ]
        };
        return JsonSerializer.Serialize(report, ReportContext.Default.Report);
    }

    public static async Task<string> MakeReportLocalAsync(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, ReleaseContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = new()
        {
            ReportDate = DateTime.Today.ToShortDateString(), 
            Versions = [ GetVersion(release) ]
        };
        return JsonSerializer.Serialize(report, ReportContext.Default.Report);
    }

    public static MajorVersion GetVersion(MajorRelease release) =>
        new()
        {
            Version = release.ChannelVersion ?? "",
            Supported = release.SupportPhase is "active" or "maintainence",
            EolDate = release.EolDate ?? "",
            SupportEndsInDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate),
            Releases = GetReleases(release).ToList()
        };

    // Get first and first security release
    public static IEnumerable<PatchRelease> GetReleases(MajorRelease majorRelease)
    {
        bool securityOnly = false;
        
        ArgumentNullException.ThrowIfNull(majorRelease.Releases);

        foreach (Release release in majorRelease.Releases)
        {
            if (securityOnly && !release.Security)
            {
                continue;
            }
            
            yield return new()
            {
                ReleaseVersion = release.ReleaseVersion,
                Security = release.Security,
                ReleaseDate = release.ReleaseDate,
                ReleasedDaysAgo = GetDaysAgo(release.ReleaseDate, true),
                CveList = release.CveList
            };

            if (release.Security)
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
   
    static int GetDaysAgo(string? date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(MajorRelease))]
public partial class ReleaseContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(Report))]
public partial class ReportContext : JsonSerializerContext
{
}

// releases.json
public class MajorRelease
{
    public string? ChannelVersion { get; set; }

    public string? LatestRelease { get; set; }

    public string? LatestReleaseDate { get; set; }

    public bool Security { get; set; }

    public string? LatestRuntime { get; set; }

    public string? LatestSdk { get; set; }

    public string? ReleaseType { get; set; }

    public string? SupportPhase { get; set; }

    public string? EolDate { get; set; }

    public List<Release>? Releases { get; set; }
}

public class Release
{
    public string? ReleaseDate { get; set; } 
    
    public string? ReleaseVersion { get; set; } 
    
    public bool Security { get; set; } 
    
    public List<Cve>? CveList { get; set; }
}

public class Cve
{
    public string? CveId { get; set; }

    public string? CveUrl { get; set; }
}


// Report JSON
public class Report
{
    public string? ReportDate { get; set; }

    public IList<MajorVersion>? Versions { get; set; }
}

public class MajorVersion
{
    public string? Version { get; set; }
    
    public bool Supported { get; set; }
    
    public string? EolDate { get; set; }
    
    public int SupportEndsInDays { get; set; }
    
    public IList<PatchRelease>? Releases { get; set; }
}

public class PatchRelease
{
    public string? ReleaseVersion { get; set; }
    
    public bool Security { get; set; }
    
    public string? ReleaseDate { get; set; }
    
    public int ReleasedDaysAgo { get; set; }
    
    public IList<Cve>? CveList { get; set; }
}
