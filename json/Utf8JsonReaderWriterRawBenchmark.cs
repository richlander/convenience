using System.Buffers;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;


namespace Utf8JsonReaderWriterRawBenchmark;
public static class Utf8JsonReaderWriterRawBenchmark
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
        var memory = new MemoryStream();
        Utf8JsonWriter utf8JsonWriterwriter = new(memory);
        var releases = await ReleasesJsonReaderReportWriter.FromStream(stream, utf8JsonWriterwriter);
        await releases.Write();
        memory.Flush();
        memory.Position= 0;
        return memory;
    }
}

public class ReleasesJsonReaderReportWriter(Pipe pipe, ReadResult result, Utf8JsonWriter writer) : JsonPipeReader(pipe, result)
{
    private readonly Utf8JsonWriter _writer = writer;

    public async Task Write()
    {
        // write head matter
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
        while (!ReadToProperty("releases"u8, false))
        {
            await Advance();
        }
        
        // Write version object
        _writer.WriteStartObject();
        WriteVersion();

        // Write release property
        _writer.WritePropertyName("releases"u8);
        _writer.WriteStartArray();

        // Write release objects
        var securityOnly = false;
        var isSecurity = false;

        while (!isSecurity)
        {
            while (!ReadToPropertyValue<bool>("security"u8, out isSecurity, false))
            {
                await Advance();
            }

            if (!isSecurity && securityOnly)
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

                continue;
            }

            _writer.WriteStartObject();
            isSecurity = WriteRelease();

            if (!isSecurity)
            {
                WriteCveEmpty();
                _writer.WriteEndObject();
                securityOnly = true;
                continue;
            }

            while (!ReadToTokenType(JsonTokenType.EndArray, false))
            {
                await Advance();
            }

            WriteCveList();
            _writer.WriteEndObject();
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

    public void WriteVersion()
    {
        var reader = GetJsonReader();

        while (reader.Read())
        {
            if (!reader.IsProperty())
            {
                continue;
            }
            else if (reader.ValueTextEquals("channel-version"u8))
            {
                _writer.WritePropertyName("version"u8);
                reader.Read();
                _writer.WriteStringValue(reader.GetValueSpan());
            }
            else if (reader.ValueTextEquals("support-phase"u8))
            {
                var supported = false;
                reader.Read();
                
                if (reader.ValueTextEquals("active"u8) ||
                    reader.ValueTextEquals("maintainence"u8))
                {
                    supported = true;
                }

                _writer.WriteBoolean("supported"u8, supported);
            }
            else if (reader.ValueTextEquals("eol-date"u8))
            {
                _writer.WritePropertyName(reader.GetValueSpan());
                reader.Read();
                var eol = reader.GetString() ?? "";
                _writer.WriteStringValue(eol);
                int days = eol is "" ? 0 : GetDaysAgo(eol, true);
                _writer.WriteNumber("support-ends-in-days"u8, days);
            }
            else if (reader.ValueTextEquals("releases"u8))
            {
                reader.Read();

                UpdateState(reader);
                return;
            }
        }

    }

    public bool WriteRelease()
    {
        bool isSecurity = false;

        var reader = GetJsonReader();
    
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                UpdateState(reader);
                return isSecurity;
            }
            else if (!reader.IsProperty())
            {
                continue;
            }
            else if (reader.ValueTextEquals("release-date"u8))
            {
                _writer.WritePropertyName(reader.GetValueSpan());
                reader.Read();
                var date = reader.GetString() ?? throw new Exception(JsonBenchmark.BADJSON);
                _writer.WriteStringValue(date);
                var days = GetDaysAgo(date, true);
                _writer.WriteNumber("released-days-ago"u8, days);
            }
            else if (reader.ValueTextEquals("release-version"u8))
            {
                _writer.WritePropertyName(reader.GetValueSpan());
                reader.Read();
                _writer.WriteStringValue(reader.GetValueSpan());
            }
            else if (reader.ValueTextEquals("security"u8))
            {
                _writer.WritePropertyName(reader.GetValueSpan());
                reader.Read();
                isSecurity = reader.GetBoolean();
                _writer.WriteBooleanValue(isSecurity);

                UpdateState(reader);
                return isSecurity;
            }
        }

        if (reader.IsFinalBlock)
        {
            return false;
        }

        throw new Exception(JsonBenchmark.BADJSONREAD);
    }

    public void WriteCveEmpty()
    {
        _writer.WritePropertyName("cve-list"u8);
        _writer.WriteStartArray();
        _writer.WriteEndArray();
    }

    public void WriteCveList()
    {
        var reader = GetJsonReader();
        _writer.WritePropertyName("cve-list"u8);
        _writer.WriteStartArray();

        while (true)
        {
            reader.Read();

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                _writer.WriteEndArray();
                return;
            }
            else if (reader.TokenType is JsonTokenType.StartObject)
            {
                _writer.WriteStartObject();
            }
            else if (reader.TokenType is JsonTokenType.EndObject)
            {
                _writer.WriteEndObject();
            }
            else if (!reader.IsProperty())
            {
                continue;
            }
            else if (reader.ValueTextEquals("cve-id"u8) || 
                     reader.ValueTextEquals("cve-url"u8))
            {
                _writer.WritePropertyName(reader.GetValueSpan());
                reader.Read();
                _writer.WriteStringValue(reader.GetValueSpan());
            }

        }

    }

    private static int GetDaysAgo(string date, bool positiveNumber = false)
    {
        bool success = DateTime.TryParse(date, out var day);
        var daysAgo = success ? (int)(day - DateTime.Now).TotalDays : 0;
        return positiveNumber ? Math.Abs(daysAgo) : daysAgo;
    }
    public static async Task<ReleasesJsonReaderReportWriter> FromStream(Stream stream, Utf8JsonWriter writer)
    {
        var (pipe, result) = await JsonPipeReader.ReadStream(stream);
        return new ReleasesJsonReaderReportWriter(pipe, result, writer);
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
        var found = ReadToProperty(ref reader, name);

        if (updateState)
        {
            UpdateState(reader);
        }

        return found;
    }

    // This app only relies on T == bool
    // A different app may rely on multiple types of T
    // This more complicated version was written to demonstrate the pattern
    public bool ReadToPropertyValue<T>(ReadOnlySpan<byte> name, [NotNullWhen(true)] out T? value, bool updateState = true)
    {
        var reader = GetJsonReader();
        var found = ReadToProperty(ref reader, name);
        value = default;

        if (found && reader.Read())
        {
            var type = typeof(T);

            if (type == typeof(bool))
            {
                // Unsafe.As<>() can be used for the same purpose
                value = (T)(object)reader.GetBoolean();
            }
            else if (type == typeof(string))
            {
                var val = reader.GetString() ?? throw new Exception("BAD JSON");
                value = (T)(object)val;
            }
            else
            {
                throw new Exception("Unsupported type");
            }

        }
        else if (found)
        {
            found = false;
        }

        if (updateState)
        {
            UpdateState(reader);
        }

        return found;
    }

    public static bool ReadToProperty(ref Utf8JsonReader reader, ReadOnlySpan<byte> name)
    {
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

    public static ReadOnlySpan<byte> GetValueSpan(this Utf8JsonReader reader) => 
        reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

    public static void WriteProperty(this Utf8JsonWriter writer, ReadOnlySpan<byte> name, string value)
    {
        writer.WritePropertyName(name);
        writer.WriteStringValue(value);
    }
}

public record Version(string MajorVersion, bool Supported, string EolDate, int SupportEndsInDays);

public record Release(string BuildVersion, bool Security, string ReleaseDate, int ReleasedDaysAgo);

public record Cve(string CveId, string CveUrl);
