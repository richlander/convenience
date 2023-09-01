using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;


namespace Utf8JsonReaderWriterBenchmark;
public static class Utf8JsonReaderWriterBenchmark
{
    public static async Task Run()
    {
        var stream = await MakeReport();

        for (int i = 0; i < stream.Length; i++)
        {
            Console.Write((char)stream.ReadByte());
        }

        Console.WriteLine();
        Console.WriteLine($"Length: {stream.Length}");
    }

    public static async Task<Stream> MakeReport()
    {
        var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(JsonBenchmark.URL, HttpCompletionOption.ResponseHeadersRead);
        var stream = await releaseMessage.Content.ReadAsStreamAsync();
        var releasesReader = await ReleasesJsonReader.FromStream(stream);
        var memory = new MemoryStream();
        var reportWriter = new ReportJsonWriter(releasesReader, memory);
        await reportWriter.Write();
        memory.Flush();
        memory.Position= 0;
        return memory;
    }
}

public class ReportJsonWriter(ReleasesJsonReader releasesReader, Stream memory)
{
    private readonly Utf8JsonWriter _writer = new(memory);
    private readonly ReleasesJsonReader _releasesReader = releasesReader;

    public async Task Write()
    {
        _writer.WriteStartObject();
        _writer.WriteString("report-date"u8, DateTime.Now.ToShortDateString());
        _writer.WriteStartArray("versions"u8);

        // Pre-read to node to validate that the buffer is large enough
        // This pattern is used throughout
        // The second (boolean) variable is used to indicate "pre-read"
        // This pattern is wasteful in that it may pre-read multiple times
        // Given this content, it makes sense.
        // Advance() will be called once at most
        // For other scenarios, retain the readerstate across buffer reads
        while (!_releasesReader.ReadToProperty("releases"u8, false))
        {
            await _releasesReader.Advance();
        }
        
        var version = _releasesReader.GetVersion();
        WriteVersionObject(version);
        _writer.WritePropertyName("releases"u8);
        _writer.WriteStartArray();

        bool securityOnly = false;

        await foreach (var release in _releasesReader.GetReleases())
        {
            if (!release.Security && securityOnly)
            {
                continue;
            }

            WriteReleaseObject(release);

            if (release.Security)
            {
                while (!_releasesReader.ReadToTokenType(JsonTokenType.EndArray, false))
                {
                    await _releasesReader.Advance();
                }

                WriteCveList();
                break;
            }
            else
            {
                WriteEmptyCveList();
                securityOnly = true;
            }
        }

        // End release
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
    }

    public void WriteCveList()
    {
        _writer.WritePropertyName("cve-list"u8);
        _writer.WriteStartArray();

        foreach (var cve in _releasesReader.GetCves())
        {
            WriteCve(cve);
        }

        _writer.WriteEndArray();
        _writer.WriteEndObject();
    }

    public void WriteEmptyCveList()
    {
        _writer.WritePropertyName("cve-list"u8);
        _writer.WriteStartArray();
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

public class ReleasesJsonReader(Pipe pipe, ReadResult result) : JsonPipeReader(pipe, result)
{        
    public Version GetVersion()
    {
        var reader = GetJsonReader();
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
                {                }

                int days = 0;
                if (eol is null)
                {
                    eol = "";
                }
                else
                {
                    days = GetDaysAgo(eol, true);
                }

                bool supported = support is "active" or "maintenance";

                if (channel is null)
                {
                    throw new Exception(JsonBenchmark.BADJSON);
                }

                UpdateState(reader);
                return new Version(channel, supported, eol, days);
            }
        }

        throw new Exception(JsonBenchmark.BADJSONREAD);
    }

    public async IAsyncEnumerable<Release> GetReleases()
    {
        int index = -1;
        while (true)
        {
            index++;

            if (index > 0)
            {
                while (!ReadToTokenType(JsonTokenType.EndArray))
                {
                    await Advance();
                }

                // Read until end of release object
                var depth = Depth -1;

                while (!ReadToDepth(depth))
                {
                    await Advance();
                }
            }

            if (!IsFinalBlock)
            {
                await Advance();
            }

            while (!ReadToProperty("cve-list"u8, false))
            {
                await Advance();
            }

            var release = GetRelease();

            if (release is null)
            {
                yield break;
            }
            else if (!release.Security && index > 0)
            {
                continue;
            }

            yield return release;
        }
    }

    public Release? GetRelease()
    {
        string? releaseDate = null;
        string? releaseVersion = null;

        var reader = GetJsonReader();
    
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                UpdateState(reader);
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

                UpdateState(reader);
                return release;
            }
        }

        if (reader.IsFinalBlock)
        {
            return null;
        }

        throw new Exception(JsonBenchmark.BADJSONREAD);
    }

    public IEnumerable<Cve> GetCves()
    {
        while(GetCve(out Cve? cve))
        {
            yield return cve;
        }

        yield break;
    }

    public bool GetCve([NotNullWhen(returnValue:true)] out Cve? cve)
    {
        string? cveId = null;
        cve = null;

        var reader = GetJsonReader();

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
                UpdateState(reader);
                return true;
            }

        }
    }

    private static int GetDaysAgo(string date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
    public static async Task<ReleasesJsonReader> FromStream(Stream stream)
    {
        var (pipe, result) = await JsonPipeReader.ReadStream(stream);
        return new ReleasesJsonReader(pipe, result);
    }
}

public class JsonPipeReader(Pipe pipe, ReadResult result)
{
    private JsonReaderState _readerState = default;
    private SequencePosition _position = default;
    private int _depth = 0;
    private long _bytesConsumed = 0;
    private Pipe _pipe = pipe;
    private ReadOnlySequence<byte> _text = result.Buffer;
    private bool _isFinalBlock = result.IsCompleted;

    public void UpdateState(Utf8JsonReader reader)
    {
        _bytesConsumed = reader.BytesConsumed;
        _position = reader.Position;
        _readerState = reader.CurrentState;
        _depth = reader.CurrentDepth;
    }

    public Utf8JsonReader GetJsonReader()
    {
        var slice = _bytesConsumed > 0 ? _text.Slice(_position) : _text;
        var reader = new Utf8JsonReader(slice, _isFinalBlock, _readerState);
        return reader;
    }

    public int Depth => _depth;

    public bool IsFinalBlock => _isFinalBlock;

    public long BytesConsumed => _bytesConsumed;

    public ReadOnlySequence<byte> Text => _text;

    public async Task Advance()
    {
        _pipe.Reader.AdvanceTo(_position);
        ReadResult result = await ReadPipe();
        _isFinalBlock = result.IsCompleted;
        _text = result.Buffer;
        _bytesConsumed = 0;
    }

    private async Task<ReadResult> ReadPipe()
    {
        var result = await _pipe.Reader.ReadAsync();
        return result;
    }

    public bool ReadToDepth(int depth, bool updateState = true)
    {
        var reader = GetJsonReader();
        var found = false;

        while (reader.Read())
        {
            if (reader.CurrentDepth <= depth)
            {
                found = true;
                break;
            }
        }

        if (updateState)
        {
            UpdateState(reader);
        }

        return found;
    }

    public bool ReadToTokenType(JsonTokenType tokenType, bool updateState = true)
    {
        var reader = GetJsonReader();
        var found = false;

        while (reader.Read())
        {
            if (reader.TokenType == tokenType)
            {
                found = true;
                break;
            }
        }

        if (updateState)
        {
            UpdateState(reader);
        }

        return found;
    }

    public bool ReadToProperty(ReadOnlySpan<byte> name, bool updateState = true)
    {
        var reader = GetJsonReader();
        var found = false;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName &&
                reader.ValueTextEquals(name))
            {
                found = true;
                break;
            }
        }

        if (updateState)
        {
            UpdateState(reader);
        }

        return found;
    }

    public static async Task<(Pipe pipe, ReadResult result)> ReadStream(Stream stream)
    {
        var pipe = new Pipe();
        _ = CopyToWriter(pipe, stream);
        var result = await pipe.Reader.ReadAsync();
        return (pipe, result);
    }

    private static async Task CopyToWriter(Pipe pipe, Stream release)
    {
        await release.CopyToAsync(pipe.Writer);
        pipe.Writer.Complete();
    }
}

public static class JsonExtensions
{
    public static bool IsProperty(this Utf8JsonReader reader) =>
        reader.TokenType is JsonTokenType.PropertyName;

    public static void WriteProperty(this Utf8JsonWriter writer, ReadOnlySpan<byte> name, string value)
    {
        writer.WritePropertyName(name);
        writer.WriteStringValue(value);
    }
}

public record Version(string MajorVersion, bool Supported, string EolDate, int SupportEndsInDays);

public record Release(string BuildVersion, bool Security, string ReleaseDate, int ReleasedDaysAgo);

public record Cve(string CveId, string CveUrl);
