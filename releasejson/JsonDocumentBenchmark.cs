using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonConfig;

namespace JsonDocumentBenchmark;

public static class JsonDocumentBenchmark
{

    private static readonly JsonSerializerOptions OPTIONS = new() { WriteIndented = false };

    public static async Task<int> RunAsync()
    {
        var json = await MakeReportAsync(JsonBenchmark.Url);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<int> RunLocalAsync()
    {
        var json = await MakeReportLocalAsync(JsonBenchmark.Path);
        Console.WriteLine(json);
        Console.WriteLine();
        return json.Length;
    }

    public static async Task<string> MakeReportAsync(string url)
    {
        // Make network call
        var httpClient = new HttpClient();
        using var responseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        var stream = await responseMessage.Content.ReadAsStreamAsync();

        // Parse Json from stream
        var doc = await JsonNode.ParseAsync(stream) ?? throw new Exception(JsonBenchmark.BADJSON);
        var version = doc["channel-version"]?.ToString() ?? "";
        var supported = doc["support-phase"]?.ToString() is "active" or "maintenance";
        var eolDate = doc["eol-date"]?.ToString() ??  "";
        var releases = doc["releases"]?.AsArray() ?? [];
        
        // Generate report
        var report = new JsonObject()
        {
            ["report-date"] = DateTime.Now.ToShortDateString(),
            ["versions"] = new JsonArray()
            {
                new JsonObject()
                {
                    ["version"] = version,
                    ["supported"] = supported,
                    ["eol-date"] = eolDate,
                    ["support-ends-in-days"] = eolDate is null ? null : GetDaysAgo(eolDate, true),
                    ["releases"] = GetReportForReleases(releases),
                }
            }
        };

        return report.ToJsonString(OPTIONS);
    }

   public static async Task<string> MakeReportLocalAsync(string path)
    {
        // Local local file
        using Stream stream = File.Open(path, FileMode.Open);

        // Parse Json from stream
        var doc = await JsonNode.ParseAsync(stream) ?? throw new Exception(JsonBenchmark.BADJSON);
        var version = doc["channel-version"]?.ToString() ?? "";
        var supported = doc["support-phase"]?.ToString() is "active" or "maintenance";
        var eolDate = doc["eol-date"]?.ToString() ??  "";
        var releases = doc["releases"]?.AsArray() ?? [];
        
        // Generate report
        var report = new JsonObject()
        {
            ["report-date"] = DateTime.Now.ToShortDateString(),
            ["versions"] = new JsonArray()
            {
                new JsonObject()
                {
                    ["version"] = version,
                    ["supported"] = supported,
                    ["eol-date"] = eolDate,
                    ["support-ends-in-days"] = eolDate is null ? null : GetDaysAgo(eolDate, true),
                    ["releases"] = GetReportForReleases(releases),
                }
            }
        };

        return report.ToJsonString(OPTIONS);
    }

    // Get first and first security release
    static JsonArray GetReportForReleases(JsonArray releases)
    {
        var securityOnly = false;
        JsonArray reportReleases = [];

        foreach (var release in releases)
        {
            if (release is null)
            {
                continue;
            }

            var releaseDate = release["release-date"]?.ToString() ?? "";
            var releaseVersion = release["release-version"]?.ToString() ?? "";
            var securityNode = release["security"] ?? new JsonObject();
            var security = securityNode.GetValueKind() switch 
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new Exception(JsonBenchmark.BADJSON)
            };

            if (securityOnly && !security)
            {
                continue;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }

            var cves = release["cve-list"] ?? throw new Exception(JsonBenchmark.BADJSON);

            var releaseObject = new JsonObject()
            {
                ["release-version"] = releaseVersion,
                ["security"] = security,
                ["release-date"] = releaseDate,
                ["released-days-ago"] = GetDaysAgo(releaseDate, true),
                ["cve-list"] = cves.DeepClone()
            };

            reportReleases.Add(releaseObject);

            if (security)
            {
                break;
            }
        }

        return reportReleases;
    }

    static int GetDaysAgo(string date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
}
