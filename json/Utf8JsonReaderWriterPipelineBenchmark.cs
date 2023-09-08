using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JsonConfig;
using JsonReaders;
using JsonExtensions;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Utf8JsonReaderWriterPipelineBenchmark;

public static class Utf8JsonReaderWriterPipelineBenchmark
{
    public static async Task<int> Run()
    {
        var stream = await MakeReport();

        for (int i = 0; i < stream.Length; i++)
        {
            Console.Write((char)stream.ReadByte());
        }

        Console.WriteLine();
        return (int)stream.Length;
    }

    public static async Task<Stream> MakeReport()
    {
        var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(JsonBenchmark.Url, HttpCompletionOption.ResponseHeadersRead);
        var stream = await releaseMessage.Content.ReadAsStreamAsync();

        // option 1
        // var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: 16 * 1024));
        // var result = await reader.ReadAsync();

        // option 2
        var pipe = new Pipe();
        var reader = pipe.Reader;
        _ = CopyToWriter(pipe, stream);
        var result = await reader.ReadAsync();

        var JsonPipeReader = new JsonPipeReader(reader, result);
        var releasesReader = new ReleasesJsonReader(JsonPipeReader);
        var memory = new MemoryStream();
        var reportWriter = new ReportJsonWriter(releasesReader, memory);
        await reportWriter.Write();
        memory.Flush();
        memory.Position= 0;
        return memory;

        static async Task CopyToWriter(Pipe pipe, Stream release)
        {
            await release.CopyToAsync(pipe.Writer);
            pipe.Writer.Complete();
        }
    }
}

public class ReportJsonWriter(ReleasesJsonReader releasesReader, Stream memory)
{
    private readonly Utf8JsonWriter _writer = new(memory);
    private readonly ReleasesJsonReader _reader = releasesReader;

    public async Task Write()
    {
        _writer.WriteStartObject();
        _writer.WriteString("report-date"u8, DateTime.Now.ToShortDateString());
        _writer.WriteStartArray("versions"u8);
        
        var version = await _reader.GetVersion();
        WriteVersionObject(version);
        _writer.WritePropertyName("releases"u8);
        // Start releases
        _writer.WriteStartArray();

        await foreach (var release in _reader.GetReleases())
        {
            WriteReleaseObject(release);
        }

        // End releases
        _writer.WriteEndArray();

        // End JSON document
        _writer.WriteEndObject();
        _writer.WriteEndArray();
        _writer.WriteEndObject();

        // Write content
        _writer.Flush();
    }

    public void WriteVersionObject(Version version)
    {
        _writer.WriteStartObject();
        _writer.WritePropertyName("version"u8);
        _writer.WriteStringValue(version.MajorVersion);
        _writer.WritePropertyName("supported"u8);
        _writer.WriteBooleanValue(version.Supported);
        _writer.WritePropertyName("eol-date"u8);
        _writer.WriteStringValue(version.EolDate);
        _writer.WritePropertyName("support-ends-in-days"u8);
        _writer.WriteNumberValue(version.SupportEndsInDays);
    }

    public void WriteReleaseObject(Release release)
    {
        _writer.WriteStartObject();
        _writer.WritePropertyName( "release-version"u8);
        _writer.WriteStringValue(release.BuildVersion);
        _writer.WritePropertyName("security");
        _writer.WriteBooleanValue(release.Security);
        _writer.WritePropertyName("release-date"u8);
        _writer.WriteStringValue(release.ReleaseDate);
        _writer.WritePropertyName("released-days-ago");
        _writer.WriteNumberValue(release.ReleasedDaysAgo);

        // Write CVE list
        _writer.WritePropertyName("cve-list"u8);
        _writer.WriteStartArray();

        foreach (var cve in release.Cves)
        {
            WriteCve(cve);
        }

        _writer.WriteEndArray();
        _writer.WriteEndObject();
    }

    public void WriteCve(Cve cve)
    {
        _writer.WriteStartObject();
        _writer.WritePropertyName("cve-id"u8);
        _writer.WriteStringValue(cve.CveId);
        _writer.WritePropertyName("cve-url"u8);
        _writer.WriteStringValue(cve.CveUrl);
        _writer.WriteEndObject();
    }
}

public class ReleasesJsonReader(JsonPipeReader reader)
{
    private readonly JsonPipeReader _json = reader;
    private ParseState _parseState = ParseState.None;

    public async Task<Version> GetVersion()
    {
        ValidateParseState(ParseState.None);

        // Pre-read to node to validate that the buffer is large enough
        // This pattern is used throughout
        // The second (boolean) variable is used to indicate "pre-read"
        // This pattern is wasteful in that it may pre-read multiple times
        // Given this content, it makes sense.
        // Advance() will be called once at most
        // For other scenarios, retain the readerstate across buffer reads
        while (!_json.ReadToProperty("releases"u8, false))
        {
            await _json.Advance();
        }

        return GetVersionObject();
    }

    private Version GetVersionObject()
    {
        var reader = _json.GetReader();
        string? channel = null;
        string? support = null;
        string? eol = null;

        while (reader.Read())
        {
            if (!reader.IsProperty())
            {
                continue;
            }
            else if (reader.ValueTextEquals("channel-version"u8))
            {
                reader.Read();
                channel = reader.GetString();
            }
            else if (reader.ValueTextEquals("support-phase"u8))
            {
                reader.Read();
                support = reader.GetString();
            }
            else if (reader.ValueTextEquals("eol-date"u8))
            {
                reader.Read();
                eol = reader.GetString();
            }
            else if (reader.ValueTextEquals("releases"u8))
            {
                reader.Read();

                if (string.IsNullOrEmpty(channel) ||
                string.IsNullOrEmpty(support))
                {                
                    throw new Exception(JsonBenchmark.BADJSON);
                }


                int days = eol is null ? 0 : GetDaysAgo(eol, true);
                eol ??= "";
                bool supported = support is "active" or "maintenance";

                if (channel is null)
                {
                    throw new Exception(JsonBenchmark.BADJSON);
                }

                _json.UpdateState(reader);
                _parseState = ParseState.Releases;
                return new Version(channel, supported, eol, days);
            }
        }

        throw new Exception(JsonBenchmark.BADJSONREAD);
    }

    public async IAsyncEnumerable<Release> GetReleases()
    {
        if (!ValidateParseState(ParseState.Releases))
        {
            while (!_json.ReadToProperty("releases"u8))
            {
                await _json.Advance();
            }
        }

        _parseState = ParseState.Release;

        // Write release objects
        var securityOnly = false;
        var isSecurity = false;

        // Assumption is that cursor is at `ObjectStart` node
        // at start of each pass
        while (!isSecurity)
        {
            while (!_json.ReadToPropertyValue<bool>("security"u8, out isSecurity, false))
            {
                await _json.Advance();
            }

            if (securityOnly && !isSecurity)
            {
                await ReadToReleaseEndObject();
                continue;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }

            var release = GetRelease();


            if (release is null)
            {
                break;
            }
            else if (release.Security)
            {
                await foreach(var cve in GetCves())
                {
                    release.Cves.Add(cve);
                }
            }

            yield return release;

            if (release.Security)
            {
                break;
            }
            else if (!await ReadToReleaseEndObject())
            {
                break;
            }
        }

        _parseState = ParseState.Done;
        yield break;

        async Task<bool> ReadToReleaseEndObject()
        {
            // Read to end of `cve-list` array
            while (!_json.ReadToTokenType(JsonTokenType.EndArray))
            {
                await _json.Advance();
            }

            // Read until end of release object
            var depth = _json.Depth -1;

            while (!_json.ReadToDepth(depth))
            {
                await _json.Advance();
            }

            return true;
        }
    }

    private Release? GetRelease()
    {
        string? releaseDate = null;
        string? releaseVersion = null;

        var reader = _json.GetReader();
    
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                _json.UpdateState(reader);
                return null;
            }
            else if (!reader.IsProperty())
            {
                continue;
            }
            else if (reader.ValueTextEquals("release-date"u8))
            {
                reader.Read();
                releaseDate = reader.GetString();
            }
            else if (reader.ValueTextEquals("release-version"u8))
            {
                reader.Read();
                releaseVersion = reader.GetString();
            }
            else if (reader.ValueTextEquals("security"u8))
            {
                reader.Read();
                var isSecurity = reader.GetBoolean();

                if (string.IsNullOrEmpty(releaseVersion) ||
                    string.IsNullOrEmpty(releaseDate))
                    {                   
                        throw new Exception(JsonBenchmark.BADJSON);
                    }

                var releaseDaysAgo = GetDaysAgo(releaseDate, true);
                var release = new Release(releaseVersion, isSecurity, releaseDate, releaseDaysAgo);

                _json.UpdateState(reader);
                return release;
            }
        }

        if (reader.IsFinalBlock)
        {
            return null;
        }

        throw new Exception(JsonBenchmark.BADJSONREAD);
    }

    private async IAsyncEnumerable<Cve> GetCves()
    {
        while (!_json.ReadToTokenType(JsonTokenType.EndArray, false))
        {
            await _json.Advance();
        }

        while(GetCve(out Cve? cve))
        {
            yield return cve;
        }

        yield break;
    }

    private bool GetCve([NotNullWhen(returnValue:true)] out Cve? cve)
    {
        string? cveId = null;
        cve = null;

        var reader = _json.GetReader();

        while (true)
        {
            reader.Read();

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                return false;
            }
            else if (!reader.IsProperty())
            {
                continue;
            }
            else if (reader.ValueTextEquals("cve-id"u8))
            {
                reader.Read();
                cveId = reader.GetString();
            }
            else if (reader.ValueTextEquals("cve-url"u8))
            {
                reader.Read();
                var cveUrl = reader.GetString();

                if (string.IsNullOrEmpty(cveUrl) ||
                    string.IsNullOrEmpty(cveId))
                {                    
                    throw new Exception(JsonBenchmark.BADJSON);
                }

                cve = new Cve(cveId, cveUrl);
                reader.Read();
                _json.UpdateState(reader);
                return true;
            }

        }
    }

    private bool ValidateParseState(ParseState expected) => expected switch
    {
        ParseState e when e == _parseState => true,
        ParseState e when e > _parseState => false,
        _ => throw new Exception(JsonBenchmark.JSONOUTOFORDER)
    };

    private static int GetDaysAgo(string date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
}

enum ParseState
{
    None,
    Releases,
    Release,
    Done
}

public record Version(string MajorVersion, bool Supported, string EolDate, int SupportEndsInDays);

public record Release(string BuildVersion, bool Security, string ReleaseDate, int ReleasedDaysAgo)
{
    public IList<Cve> Cves { get; init; } = [];
};

public record Cve(string CveId, string CveUrl);
