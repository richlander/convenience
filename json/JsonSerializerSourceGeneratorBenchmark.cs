using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSerializerSourceGeneratorBenchmark;
public static class JsonSerializerSourceGeneratorBenchmark
{
    public static async Task<string> Run()
    {
        var message = "JSON data is wrong";
        HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync<ReleaseJson.Release>(FakeTestData.URL, ReleaseContext.Default.Release) ?? throw new Exception(message);
        var version = GetVersionForRelease(release);

        List<ReportJson.Version> versions= [version];
        ReportJson.ReleaseReport report = new(DateTime.Today.ToShortDateString(), versions);
        return JsonSerializer.Serialize(report, ReportContext.Default.ReleaseReport);
    }

    public static ReportJson.Version GetVersionForRelease(ReleaseJson.Release release)
    {
        List<ReportJson.Release> releases = [];
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        ReportJson.Version version = new(release.ChannelVersion, release.SupportPhase is "active" or "maintainence", release.EolDate ?? "Unknown", supportDays, releases);
        bool securityOnly = false;
        
        foreach (ReleaseJson.ReleaseDetail releaseDetail in release.Releases)
        {
            if (!releaseDetail.Security && securityOnly)
            {
                continue;
            }
            
            var reportRelease = new ReportJson.Release(releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.Cves);
            releases.Add(reportRelease);

            if (releaseDetail.Security)
            {
                break;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }
        }

        return version;

        static int GetDaysAgo(string date, bool positiveNumber = false)
        {
            bool success = DateTime.TryParse(date, out var day);
            var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
            return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
        }
    }   
}

[JsonSerializable(typeof(ReleaseJson.Release))]
public partial class ReleaseContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(ReportJson.ReleaseReport))]
public partial class ReportContext : JsonSerializerContext
{
}
