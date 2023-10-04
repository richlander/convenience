using JsonBenchmark;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NewtonsoftJsonSerializerBenchmark;

public class NewtonsoftJsonSerializerBenchmark
{
    public static async Task<int> MakeReportWebAsync(string url)
    {
        // Make network call
        using var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        using var stream = await releaseMessage.Content.ReadAsStreamAsync();

        // Attach stream to serializer
        JsonSerializer serializer = new();
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);

        // Process JSON
        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception(Error.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson = JsonConvert.SerializeObject(report);
        WriteJsonToConsole(reportJson);
        return reportJson.Length;
    }

    public static int MakeReportFile(string file)
    {
        // Local local file
        using Stream stream = File.Open(file, FileMode.Open);

        // Attach stream to serializer
        JsonSerializer serializer = new();
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);

        // Process JSON
        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception(Error.BADJSON);
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        string reportJson = JsonConvert.SerializeObject(report);
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

// releases.json
[JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
public record MajorRelease(string ChannelVersion, string LatestRelease, string LatestReleaseDate, bool Security, string LatestRuntime, string LatestSdk, string ReleaseType, string SupportPhase, string EolDate, string ReleasesJson, List<Release> Releases);

[JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
public record Release(string ReleaseDate, string ReleaseVersion, bool Security, List<Cve> CveList);

[JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
public record Cve(string CveId,string CveUrl);

[JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
public record Report(string ReportDate, IList<MajorVersion> Versions);

[JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
public record MajorVersion(string Version,  bool Supported, string EolDate, int SupportEndsInDays, IList<PatchRelease> Releases);

[JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
public record PatchRelease(string ReleaseDate, int ReleasedDaysAgo,string ReleaseVersion, bool Security, IList<Cve> CveList);
