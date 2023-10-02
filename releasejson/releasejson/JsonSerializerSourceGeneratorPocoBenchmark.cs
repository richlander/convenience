using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonBenchmark;

namespace JsonSerializerSourceGeneratorPocoBenchmark;

public static class JsonSerializerSourceGeneratorPocoBenchmark
{
    // Benchmark for JSON via Web URL
    public static async Task<int> MakeReportWebAsync(string url)
    {
        using HttpClient httpClient= new();
        MajorRelease release = await httpClient.GetFromJsonAsync(url, ReleaseContext.Default.MajorRelease) ?? throw new Exception(Error.BADJSON);
        Report report = new()
        {
            ReportDate = DateTime.Today.ToShortDateString(), 
            Versions = [ GetVersion(release) ]
        };
        string reportJson = JsonSerializer.Serialize(report, ReportContext.Default.Report);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    // Benchmark for JSON via file
    public static async Task<int> MakeReportFileAsync(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, ReleaseContext.Default.MajorRelease) ?? throw new Exception(Error.BADJSON);
        Report report = new()
        {
            ReportDate = DateTime.Today.ToShortDateString(), 
            Versions = [ GetVersion(release) ]
        };
        string reportJson = JsonSerializer.Serialize(report, ReportContext.Default.Report);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    // Benchmark for JSON via string
    public static int MakeReportMemory(string json)
    {
        MajorRelease release = JsonSerializer.Deserialize<MajorRelease>(json, ReleaseContext.Default.MajorRelease)!;
        Report report = new()
        {
            ReportDate = DateTime.Today.ToShortDateString(), 
            Versions = [ GetVersion(release) ]
        };
        string reportJson = JsonSerializer.Serialize(report, ReportContext.Default.Report);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
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

    static void WriteJsonToConsole(string json)
    {
#if DEBUG
        Console.WriteLine(json);
        Console.WriteLine();
#endif
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
