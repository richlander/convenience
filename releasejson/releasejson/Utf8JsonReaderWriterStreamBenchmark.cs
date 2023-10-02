using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JsonBenchmark;
using JsonReaders;
using JsonExtensions;


namespace Utf8JsonReaderWriterStreamBenchmark;

public static class Utf8JsonReaderWriterStreamBenchmark
{
    public static async Task<int> MakeReportWebAsync(string url)
    {
        // Make network call
        using var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        releaseMessage.EnsureSuccessStatusCode();
        using var jsonStream = await releaseMessage.Content.ReadAsStreamAsync();

        // Acquire byte[] as a buffer for the Stream 
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(JsonStreamReader.Size);
        int read = await jsonStream.ReadAsync(rentedArray);

        // Process JSON
        var releasesReader = new ReleasesJsonReader(new(jsonStream, rentedArray, read));
        var memory = new MemoryStream();
        var reportWriter = new ReportJsonWriter(releasesReader, memory);
        await reportWriter.Write();
        ArrayPool<byte>.Shared.Return(rentedArray);

        // Flush stream and prepare for reader
        memory.Flush();
        memory.Position= 0;

        WriteJsonToConsole(memory);
        return (int)memory.Length;
    }

    public static async Task<int> MakeReportFileAsync(string path)
    {
        // Local local file
        using Stream stream = File.Open(path, FileMode.Open);

        // Acquire byte[] as a buffer for the Stream 
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(JsonStreamReader.Size);
        int read = await stream.ReadAsync(rentedArray);

        // Process JSON
        var releasesReader = new ReleasesJsonReader(new(stream, rentedArray, read));
        var memory = new MemoryStream();
        var reportWriter = new ReportJsonWriter(releasesReader, memory);
        await reportWriter.Write();
        ArrayPool<byte>.Shared.Return(rentedArray);

        // Flush stream and prepare for reader
        memory.Flush();
        memory.Position= 0;

        WriteJsonToConsole(memory);
        return (int)memory.Length;
    }

    public static void WriteJsonToConsole(Stream stream)
    {
#if DEBUG
        for (int i = 0; i < stream.Length; i++)
        {
            Console.Write((char)stream.ReadByte());
        }

        Console.WriteLine();
#endif
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
        
        var version = await _reader.GetVersionAsync();
        WriteVersionObject(version);
        _writer.WritePropertyName("releases"u8);
        // Start releases
        _writer.WriteStartArray();

        await foreach (var release in _reader.GetReleasesAsync())
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

public class ReleasesJsonReader(JsonStreamReader reader)
{
    private readonly JsonStreamReader _json = reader;
    private ParseState _parseState = ParseState.None;

    public async Task<Version> GetVersionAsync()
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
            await _json.AdvanceAsync();
        }

        return GetVersion();
    }

    private Version GetVersion()
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
                    throw new Exception(Error.BADJSON);
                }


                int days = eol is null ? 0 : GetDaysAgo(eol, true);
                eol ??= "";
                bool supported = support is "active" or "maintenance";

                if (channel is null)
                {
                    throw new Exception(Error.BADJSON);
                }

                _json.UpdateState(reader);
                _parseState = ParseState.Releases;
                return new Version(channel, supported, eol, days);
            }
        }

        throw new Exception(Error.BADJSONREAD);
    }

    public async IAsyncEnumerable<Release> GetReleasesAsync()
    {
        if (!ValidateParseState(ParseState.Releases))
        {
            while (!_json.ReadToProperty("releases"u8))
            {
                await _json.AdvanceAsync();
            }
        }

        _parseState = ParseState.Release;

        var securityOnly = false;

        while (!_json.ReadToTokenType(JsonTokenType.StartObject))
        {
            await _json.AdvanceAsync();
        }

        // Write release objects
        // Assumption is that cursor is at `ObjectStart` node
        // at start of each pass
        while (true)
        {
            var isSecurity = false;

            while (!_json.ReadToPropertyValue<bool>("security"u8, out isSecurity, false))
            {
                await _json.AdvanceAsync();
            }

            if (securityOnly && !isSecurity)
            {
                if (!await ReadToReleaseStartObject())
                {
                    break;
                }

                continue;
            }
            else if (!securityOnly)
            {
                securityOnly = true;
            }

            var release = GetRelease();

            if (release.Security)
            {
                await foreach(var cve in GetCvesAsync())
                {
                    release.Cves.Add(cve);
                }
            }

            yield return release;

            if (release.Security)
            {
                break;
            }
            else if (!await ReadToReleaseStartObject())
            {
                break;
            }
        }

        _parseState = ParseState.Done;
        yield break;

        async Task<bool> ReadToReleaseStartObject()
        {

            // Read to next property to ensure depth is at property not a value
            while (!_json.ReadToTokenType(JsonTokenType.PropertyName))
            {
                await _json.AdvanceAsync();
            }

            // Read until end of release object
            var depth = _json.Depth -1;

            while (!_json.ReadToDepth(depth))
            {
                await _json.AdvanceAsync();
            }

            JsonTokenType tokenType;

            while (!_json.ReadNext(out tokenType))
            {
                await _json.AdvanceAsync();
            }

            if (tokenType is JsonTokenType.StartObject)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private Release GetRelease()
    {
        string? releaseDate = null;
        string? releaseVersion = null;

        var reader = _json.GetReader();
    
        while (reader.Read())
        {
            if (!reader.IsProperty())
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
                        throw new Exception(Error.BADJSON);
                    }

                var releaseDaysAgo = GetDaysAgo(releaseDate, true);
                var release = new Release(releaseDate, releaseDaysAgo, releaseVersion, isSecurity);

                _json.UpdateState(reader);
                return release;
            }
        }

        throw new Exception(Error.BADJSONREAD);
    }

    private async IAsyncEnumerable<Cve> GetCvesAsync()
    {
        while (!_json.ReadToTokenType(JsonTokenType.EndArray, false))
        {
            await _json.AdvanceAsync();
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
                    throw new Exception(Error.BADJSON);
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
        _ => throw new Exception(Error.JSONOUTOFORDER)
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

public record Release(string ReleaseDate, int ReleasedDaysAgo, string BuildVersion, bool Security)
{
    public IList<Cve> Cves { get; init; } = [];
};

public record Cve(string CveId, string CveUrl);
