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
    public static async Task<int> Run()
    {
        var json = await MakeReport();
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReport()
    {
        HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync<MajorRelease>(JsonBenchmark.Url, ReleaseRecordContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        bool supported = release.SupportPhase is "active" or "maintainence";
        Version version = new(release.ChannelVersion, supported, release.EolDate ?? "Unknown", supportDays, GetReleasesForReport(release).ToList());
        Report report = new(DateTime.Today.ToShortDateString(), [version]);
        return JsonSerializer.Serialize(report, ReportRecordContext.Default.Report);
    }

    // Get first and first security release
    public static IEnumerable<Release> GetReleasesForReport(MajorRelease release)
    {
        bool securityOnly = false;
        
        foreach (ReleaseDetail releaseDetail in release.Releases)
        {
            if (!releaseDetail.Security && securityOnly)
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