using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonDocumentBenchmark;
public static class JsonDocumentBenchmark
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
        // Make network call
        var httpClient = new HttpClient();
        using var responseMessage = await httpClient.GetAsync(JsonBenchmark.Url, HttpCompletionOption.ResponseHeadersRead);
        var stream = await responseMessage.Content.ReadAsStreamAsync();

        // Parse Json from stream
        var doc = JsonNode.Parse(stream) ?? throw new Exception(JsonBenchmark.BADJSON);
        var version = doc["channel-version"]?.ToString() ?? throw new Exception(JsonBenchmark.BADJSON);
        var supportPhase = doc["support-phase"]?.ToString() ?? throw new Exception(JsonBenchmark.BADJSON);
        var supported = supportPhase is "active" or "maintenance";
        var eolDate = doc["eol-date"]?.ToString();
        var releases = doc["releases"]?.AsArray() ?? throw new Exception(JsonBenchmark.BADJSON);
        
        // Generate report
        var reportReleaseArray = new JsonArray();

        foreach(var releaseReport in GetReleasesForReport(releases))
        {
            reportReleaseArray.Add(releaseReport);
        }

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
                    ["releases"] = reportReleaseArray,
                }
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = false };
        return report.ToJsonString(options);
    }

    // Get first and first security release
    static IEnumerable<JsonObject> GetReleasesForReport(JsonArray releases)
    {
        var securityOnly = false;

        foreach (var releaseVal in releases)
        {
            var release = releaseVal ?? throw new Exception(JsonBenchmark.BADJSON);
            var releaseDate = release["release-date"]?.ToString() ?? throw new Exception(JsonBenchmark.BADJSON);
            var releaseVersion = release["release-version"]?.ToString() ?? throw new Exception(JsonBenchmark.BADJSON);
            var securityNode = release["security"] ?? throw new Exception(JsonBenchmark.BADJSON);
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

            var reportRelease = new JsonObject()
            {
                ["release-version"] = releaseVersion,
                ["security"] = security,
                ["release-date"] = releaseDate,
                ["released-days-ago"] = GetDaysAgo(releaseDate, true),
                ["cve-list"] = cves.DeepClone()
            };

            yield return reportRelease;

            if (security)
            {
                yield break;
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
