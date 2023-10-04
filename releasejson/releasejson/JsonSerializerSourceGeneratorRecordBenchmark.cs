using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonBenchmark;
using ReleaseJson;
using ReportJson;

namespace JsonSerializerSourceGeneratorRecordBenchmark;

public static class JsonSerializerSourceGeneratorRecordBenchmark
{
    // Benchmark for JSON via Web URL
    public static async Task<int> MakeReportWebAsync(string url)
    {
        using HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync(url, ReleaseRecordContext.Default.MajorRelease) ?? throw new Exception(Error.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson = JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    // Benchmark for jSON via file
    public static async Task<int> MakeReportFileAsync(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        MajorRelease release = await JsonSerializer.DeserializeAsync<MajorRelease>(stream, ReleaseRecordContext.Default.MajorRelease) ?? throw new Exception(Error.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson = JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    // Benchmark for JSON via string
    public static int MakeReportMemory(string json)
    {
        MajorRelease release = JsonSerializer.Deserialize<MajorRelease>(json, ReleaseRecordContext.Default.MajorRelease)!;
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson = JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
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
public partial class ReleaseRecordContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(Report))]
public partial class ReportRecordContext : JsonSerializerContext
{
}