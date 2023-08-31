using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReleaseJson;
using ReportJson;
using Version = ReportJson.Version;

namespace JsonSerializerSourceGeneratorBenchmark;
public static class JsonSerializerSourceGeneratorBenchmark
{
    public static async Task Run()
    {
        var json = await MakeReport();
        Console.WriteLine(json);
        Console.WriteLine();
        Console.WriteLine($"Length: {json.Length}");
    }

    public static async Task<string> MakeReport()
    {
        HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync<MajorRelease>(JsonBenchmark.URL, ReleaseContext.Default.MajorRelease) ?? throw new Exception(JsonBenchmark.BADJSON);

        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        Version version = new(release.ChannelVersion, release.SupportPhase is "active" or "maintainence", release.EolDate ?? "Unknown", supportDays, []);

        foreach(var reportRelease in GetReleasesForReport(release))
        {
            version.Releases.Add(reportRelease);
        }
        
        Report report = new(DateTime.Today.ToShortDateString(), [version]);
        return JsonSerializer.Serialize(report, ReportContext.Default.Report);
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
            
            var reportRelease = new Release(releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.Cves);
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
