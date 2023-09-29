using System.Net.Http.Json;
using System.Text.Json;
using ReportJson;
using ReleaseJson;
using MajorVersion = ReportJson.MajorVersion;
using JsonConfig;

namespace JsonSerializerBenchmark;

public class JsonSerializerBenchmark
{
    private static readonly JsonSerializerOptions OPTIONS = new() { PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower };

    public static async Task<int> RunAsync()
    {
        var json = await MakeReportAsync(JsonBenchmark.Url);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<int> RunLocalAsync()
    {
        var json = await MakeReportLocalAsync(JsonBenchmarkLocal.Path);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReportAsync(string url)
    {
        using HttpClient httpClient= new();
        MajorRelease release = await httpClient.GetFromJsonAsync<MajorRelease>(url, OPTIONS) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, OPTIONS);
    }

    public static async Task<string> MakeReportLocalAsync(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, OPTIONS) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, OPTIONS);
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
}
