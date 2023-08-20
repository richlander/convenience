using System.Net.Http.Json;
using System.Text.Json;

namespace JsonSerializerBenchmark;
public static class JsonSerializerBenchmark
{
    public static async Task<string> Run()
    {
        var message = "JSON data is wrong";
        HttpClient httpClient= new();
        var release = await httpClient.GetFromJsonAsync<ReleaseJson.Release>(FakeTestData.URL) ?? throw new Exception(message);
        var version = GetVersionForRelease(release);

        var options = new JsonSerializerOptions(JsonSerializerOptions.Default);
        List<ReportJson.Version> versions= [version];
        ReportJson.ReleaseReport report = new(DateTime.Today.ToShortDateString(), versions);
        return JsonSerializer.Serialize(report, options);
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
