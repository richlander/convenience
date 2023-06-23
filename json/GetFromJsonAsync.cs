using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Report;
using Version = Report.Version;

namespace ReleaseJson;
public static class GetFromJson
{
    public static async Task<string> Go()
    {
        List<Version> versions= new();
        await foreach (var release in   GetReleases())
        {
            versions.Add(PrintRelease(release));
        }

        ReleaseReport report = new(DateTime.Today.ToShortDateString(), versions);

        return JsonSerializer.Serialize(report);
    }

    public static async IAsyncEnumerable<Release> GetReleases()
    {
        HttpClient  httpClient = new();
        string loadError = "Failed to load release information.";
        var releases = await httpClient.GetFromJsonAsync<ReleaseIndex>(ReleaseValues.RELEASE_INDEX_URL) ?? throw new Exception(loadError);

        foreach (var releaseSummary in releases.ReleasesIndex)
        {
            if (DateOnly.TryParse(releaseSummary.EolDate, out DateOnly eolDate)
             && DateOnly.FromDateTime(DateTime.Now.AddYears(-1)).DayNumber > eolDate.DayNumber)
            {
                continue;
            }

            var release = await httpClient.GetFromJsonAsync<Release>(releaseSummary.ReleasesJson);
            if (release is not null)
            {
                yield return release;
            }
            else
            {
                yield break;
            }
        }

        yield break;
    }

    public static Version PrintRelease(Release release)
    {
        int supportDays = release.EolDate is null ? 0 : GetDaysAgo(release.EolDate);
        List<Report.Release> releases = new();
        Version version = new(release.ChannelVersion, release.SupportPhase is "active" or "maintainence", release.EolDate ?? "Unknown", supportDays, releases);
        bool printedSecurity = false;
        bool printedLatest = false;
        
        foreach (ReleaseDetail releaseDetail in release.Releases)
        {
            if (printedSecurity)
            {
                break;
            }
            else if (printedLatest && !releaseDetail.Security)
            {
                continue;
            }
            
            var r = new Report.Release(releaseDetail.ReleaseVersion, releaseDetail.Security, releaseDetail.ReleaseDate, GetDaysAgo(releaseDetail.ReleaseDate, true), releaseDetail.Cves);
            releases.Add(r);

            printedSecurity = releaseDetail.Security;
            printedLatest = true;
        }

        return version;
    }   

    public static int GetDaysAgo(string date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
}
