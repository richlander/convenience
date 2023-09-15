using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonConfig;
using ReleaseJson;
using ReportJson;
using Version = ReportJson.Version;

namespace JsonSerializerSourceGeneratorRecordBenchmark;

public static class JsonSerializerSourceGeneratorRecordBenchmark
{
    public static async Task<int> RunAsync()
    {
        var json = await MakeReportAsync();
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReportAsync()
    {
        using HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync<MajorRelease>(JsonBenchmark.Url, ReleaseRecordContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
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
            
            var reportRelease = new Release(releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.Cves);
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
public partial class ReleaseRecordContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(Report))]
public partial class ReportRecordContext : JsonSerializerContext
{
}