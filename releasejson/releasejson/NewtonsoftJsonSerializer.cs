using System.Diagnostics.CodeAnalysis;
using JsonConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NewtonsoftJsonSerializerBenchmark;

public class NewtonsoftJsonSerializerBenchmark
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
        var json = MakeReportLocalAsync(BenchmarkData.Path);
        Console.WriteLine(json);
        Console.WriteLine();
        // This is here to maintain the same signature as the other test methods
        // Because Json.NET doesn't have an async serializer
        await Task.CompletedTask;
        return json.Length;
    }

    public static async Task<string> MakeReportAsync(string url)
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
        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception();
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonConvert.SerializeObject(report);
    }

    public static string MakeReportLocalAsync(string file)
    {
        // Local local file
        using Stream stream = File.Open(file, FileMode.Open);

        // Attach stream to serializer
        JsonSerializer serializer = new();
        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);

        // Process JSON
        MajorRelease release = serializer.Deserialize<MajorRelease>(reader) ?? throw new Exception();
        Report report = new(DateTime.Today.ToShortDateString(), [ GetVersion(release) ]);
        return JsonConvert.SerializeObject(report);
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
