using System.Net.Http.Json;
using System.Text.Json;
using JsonBenchmark;
using ReportJson;
using ReleaseJson;

namespace JsonSerializerBenchmark;

public class JsonSerializerBenchmark
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower };

    // Benchmark for JSON via Web URL
    public static async Task<int> MakeReportWebAsync(string url)
    {
        using HttpClient httpClient= new();
        MajorRelease release = await httpClient.GetFromJsonAsync<MajorRelease>(url, _options) ?? throw new Exception(Error.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson =  JsonSerializer.Serialize(report, _options);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    // Benchmark for JSON via file
    public static async Task<int> MakeReportFileAsync(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, _options) ?? throw new Exception(Error.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson =  JsonSerializer.Serialize(report, _options);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    // Benchmark for JSON via string
    public static int MakeReportMemory(string json)
    {
        MajorRelease release = JsonSerializer.Deserialize<MajorRelease>(json, _options)!;
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson =  JsonSerializer.Serialize(report, _options);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    public static MajorVersion GetVersion(MajorRelease release) =>
        new(release.ChannelVersion, 
            release.SupportPhase is "active" or "maintainence", 
            release.EolDate ?? "", 
            release.EolDate is null ? 0 : GetDaysAgo(release.EolDate), 
            GetReleases(release).ToList()
            );

    // Get first and first security release
    public static IEnumerable<ReportJson.PatchRelease> GetReleases(MajorRelease majorRelease)
    {
        bool securityOnly = false;
        
        foreach (ReleaseJson.Release release in majorRelease.Releases)
        {
            if (securityOnly && !release.Security)
            {
                continue;
            }
            
            yield return new(release.ReleaseDate, GetDaysAgo(release.ReleaseDate, true), release.ReleaseVersion, release.Security, release.CveList);

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
   
    static int GetDaysAgo(string date, bool positiveNumber = false)
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
