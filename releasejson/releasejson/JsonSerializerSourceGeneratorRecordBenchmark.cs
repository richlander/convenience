using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonConfig;
using ReleaseJson;
using ReportJson;
using MajorVersion = ReportJson.MajorVersion;

namespace JsonSerializerSourceGeneratorRecordBenchmark;

public static class JsonSerializerSourceGeneratorRecordBenchmark
{
    public static async Task<int> RunAsync()
    {
        var json = await MakeReportAsync(BenchmarkData.Url);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<int> RunLocalAsync()
    {
        var json = await MakeReportLocalAsync(BenchmarkData.Path);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReportAsync(string url)
    {
        using HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync<MajorRelease>(url, ReleaseRecordContext.Default.MajorRelease) ?? throw new Exception(BenchmarkData.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
    }

    public static async Task<string> MakeReportLocalAsync(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, ReleaseRecordContext.Default.MajorRelease) ?? throw new Exception(BenchmarkData.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
    }

    public static MajorVersion GetVersion(MajorRelease release) =>
        new(release.ChannelVersion, 
            release.SupportPhase is "active" or "maintainence", 
            release.EolDate ?? "", 
            release.EolDate is null ? 0 : GetDaysAgo(release.EolDate), 
            GetReleases(release).ToList()
            );

    // Get first and first security release
    public static IEnumerable<PatchRelease> GetReleases(MajorRelease majorRelease)
    {
        bool securityOnly = false;
        
        foreach (Release release in majorRelease.Releases)
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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(MajorRelease))]
public partial class ReleaseRecordContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(Report))]
public partial class ReportRecordContext : JsonSerializerContext
{
}