using System.Buffers;
using System.Text.Json;
using JsonBenchmark;
using JsonReaders;
using JsonExtensions;


namespace Utf8JsonReaderWriterStreamRawBenchmark;

public static class Utf8JsonReaderWriterStreamRawBenchmark
{
    public static async Task<int> MakeReportWebAsync(string url)
    {
        // Make network call
        using var httpClient = new HttpClient();
        using var releaseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        var stream = await releaseMessage.Content.ReadAsStreamAsync();

        // Acquire byte[] as a buffer for the Stream 
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(JsonStreamReader.Size);

        // Process JSON
        var memory = new MemoryStream();
        Utf8JsonWriter utf8JsonWriterwriter = new(memory);
        var releases = await ReleasesJsonReaderReportWriter.FromStream(stream, rentedArray, utf8JsonWriterwriter);
        await releases.Write();
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

        // Process JSON
        var memory = new MemoryStream();
        Utf8JsonWriter utf8JsonWriterwriter = new(memory);
        var releases = await ReleasesJsonReaderReportWriter.FromStream(stream, rentedArray, utf8JsonWriterwriter);
        await releases.Write();
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

public class ReleasesJsonReaderReportWriter(Stream stream, byte[] buffer, int readCount, Utf8JsonWriter writer) : JsonStreamReader(stream, buffer, readCount)
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
            await AdvanceAsync();
        }
        
        // Write version object
        _writer.WriteStartObject();
        WriteVersion();

        // Write release property
        _writer.WritePropertyName("releases"u8);
        _writer.WriteStartArray();

        var securityOnly = false;

        while (!ReadToTokenType(JsonTokenType.StartObject))
        {
            await AdvanceAsync();
        }

        // Write release objects
        while (true)
        {
            var isSecurity = false;

            while(!ReadToPropertyValue<bool>("security"u8, out isSecurity, false))
            {
                await AdvanceAsync();
            }

            if (!isSecurity && securityOnly)
            {
                if (!await ReadToReleaseStartObject())
                {
                    break;
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
                await ReadToReleaseStartObject();
                continue;
            }

            while (!ReadToTokenType(JsonTokenType.EndArray, false))
            {
                await AdvanceAsync();
            }

            WriteCveList();
            _writer.WriteEndObject();
            break;
        }

        // End release
        _writer.WriteEndArray();

        // End JSON document
        _writer.WriteEndObject();
        _writer.WriteEndArray();
        _writer.WriteEndObject();

        // Write content
        _writer.Flush();

        async Task<bool> ReadToReleaseStartObject()
        {

            // Read to next property to ensure depth is at property not a value
            while (!ReadToTokenType(JsonTokenType.PropertyName))
            {
                await AdvanceAsync();
            }

            // Read until end of release object
            var depth = Depth -1;

            while (!ReadToDepth(depth))
            {
                await AdvanceAsync();
            }

            JsonTokenType tokenType;

            while (!ReadNext(out tokenType))
            {
                await AdvanceAsync();
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

    public void WriteVersion()
    {
        var reader = GetReader();

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

        var reader = GetReader();
    
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
                var date = reader.GetString() ?? throw new Exception(Error.BADJSON);
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

        throw new Exception(Error.BADJSONREAD);
    }

    public void WriteCveEmpty()
    {
        _writer.WritePropertyName("cve-list"u8);
        _writer.WriteStartArray();
        _writer.WriteEndArray();
    }

    public void WriteCveList()
    {
        var reader = GetReader();
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
    public static async Task<ReleasesJsonReaderReportWriter> FromStream(Stream stream, byte[] buffer, Utf8JsonWriter writer)
    {
        int read = await stream.ReadAsync(buffer);
        return new ReleasesJsonReaderReportWriter(stream, buffer, read, writer);
    }
}
