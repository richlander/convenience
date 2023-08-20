using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonDocumentBenchmark;
public static class JsonDocumentBenchmark
{
    public static async Task<string> Run()
    {
        var message = "JSON content missing";
        var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(FakeTestData.URL, HttpCompletionOption.ResponseHeadersRead);
        var stream = await releaseMessage.Content.ReadAsStreamAsync();

        var doc = JsonNode.Parse(stream) ?? throw new Exception(message);

        var version = doc["channel-version"]?.ToString() ?? throw new Exception(message);
        var supportPhase = doc["support-phase"]?.ToString() ?? throw new Exception(message);
        var eolDate = doc["eol-date"]?.ToString();
        var releases = doc["releases"]?.AsArray() ?? throw new Exception(message);
        var supported = supportPhase is "active" or "maintenance";
        var reportReleaseArray = new JsonArray();
        var securityOnly = false;

        foreach (var releaseVal in releases)
        {
            var release = releaseVal ?? throw new Exception(message);

            var releaseDate = release["release-date"]?.ToString() ?? throw new Exception(message);
            var releaseVersion = release["release-version"]?.ToString() ?? throw new Exception(message);
            var securityNode = release["security"] ?? throw new Exception(message);
            var security = securityNode.GetValueKind() switch 
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new Exception(message)
            };

            if (securityOnly && !security)
            {
                continue;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }

            var cves = release["cve-list"] ?? throw new Exception(message);

            var reportRelease = new JsonObject()
            {
                ["release-version"] = releaseVersion,
                ["security"] = security,
                ["release-date"] = releaseDate,
                ["released-days-ago"] = GetDaysAgo(releaseDate, true),
                ["cve-list"] = cves.DeepClone()
            };

            reportReleaseArray.Add(reportRelease);

            if (security)
            {
                break;
            }
        }

        var reportVersion = new JsonObject()
        {
            ["version"] = version,
            ["supported"] = supported,
            ["eol-date"] = eolDate
        };

        var report = new JsonObject()
        {
            ["report-date"] = DateTime.Now.ToShortDateString(),
            ["versions"] = new JsonArray()
            {
                reportVersion
            }
        };

        if (eolDate is not null)
        {
            reportVersion.Add("support-ends-in-days", GetDaysAgo(eolDate, true));
        }

        reportVersion.Add("releases", reportReleaseArray);
        var options = new JsonSerializerOptions { WriteIndented = false };
        return report.ToJsonString(options);

        static int GetDaysAgo(string date, bool positiveNumber = false)
        {
            bool success = DateTime.TryParse(date, out var day);
            var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
            return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
        }
    }
}


