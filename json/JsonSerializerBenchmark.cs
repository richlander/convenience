using System.Net.Http.Json;
using System.Text.Json;
using ReportJson;
using ReleaseJson;
using Version = ReportJson.Version;

namespace JsonSerializerBenchmark;
public class JsonSerializerBenchmark
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
        var release = await httpClient.GetFromJsonAsync<MajorRelease>(JsonBenchmark.URL) ?? throw new Exception(JsonBenchmark.BADJSON);
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        Version version = new(release.ChannelVersion, release.SupportPhase is "active" or "maintainence", release.EolDate ?? "Unknown", supportDays, []);

        foreach(var reportRelease in GetReleasesForReport(release))
        {
            version.Releases.Add(reportRelease);
        }
        
        Report report = new(DateTime.Today.ToShortDateString(), [version]);
        var json = JsonSerializer.Serialize(report);
        return json;
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
            
            yield return new Release(releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.Cves);

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
