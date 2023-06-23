using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Columns;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftAntimalwareEngine;
// using Report;
using Version = Report.Version;

namespace ReleaseJson;
public static class JsonWithUtf8
{
    private const int SIZE = 1024 * 4;
    public static async Task Go()
    {
        MemoryStream memoryStream = new();
        Utf8JsonWriter writer = new(memoryStream);
        HttpClient httpClient = new();
        string gist = "https://gist.githubusercontent.com/richlander/37f936a4e65d2176236c299885b84ab4/raw/7dead81c585da60504a4abd5a264db99544fd5ed/release-index.json";
        var message = await httpClient.GetAsync(gist, HttpCompletionOption.ResponseContentRead);
        var releases = message.Content.ReadAsStream();

        WriteHeadMatter(writer);

        foreach (var url in GetUrls(releases))
        {
            // Console.WriteLine(url);
            var releaseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            var release = releaseMessage.Content.ReadAsStream();
            GetReportForVersion(release, writer);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
        memoryStream.Flush();
        memoryStream.Position= 0;
        StreamReader reader = new(memoryStream);
        Console.WriteLine(reader.ReadToEnd());
    }

    private static void 

    public static void WriteHeadMatter(Utf8JsonWriter writer)
    {
        var reportDateProp = "report-date"u8;
        var versionsProp = "versions"u8;

        writer.WriteStartObject();
        writer.WriteString(reportDateProp, DateTime.Now.ToShortDateString());
        writer.WriteStartArray(versionsProp);
    }
    public static void GetReportForVersion(Stream stream, Utf8JsonWriter writer)
    {
        var channel = "channel-version"u8;
        var support = "support-phase"u8;
        var maintenance = "maintenance"u8;
        var active = "active"u8;
        var eol = "eol-date"u8;
        var supportEnds = "support-ends-in-days"u8;
        var releases = "releases"u8;

        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(SIZE);
        Span<byte> buffer = rentedArray;
        int read = stream.Read(buffer);
        ReadOnlySpan<byte> text = buffer.Slice(0, read);
        var reader = new Utf8JsonReader(text, isFinalBlock: false, state: default);

        writer.WriteStartObject();

        while (reader.BytesConsumed < text.Length && reader.Read())
        {
            if (IsMatchProperty(reader, channel) || IsMatchProperty(reader, support))
            {                                                                                                                                               
                writer.WritePropertyName(reader.ValueSpan);
                if (reader.Read())
                {
                    writer.WriteStringValue(reader.ValueSpan);
                } 
            }
            else if (IsMatchProperty(reader, eol))
            {
                writer.WritePropertyName(reader.ValueSpan);
                if (reader.Read())
                {
                    writer.WriteStringValue(reader.ValueSpan);

                    string? dateString;
                    if ((dateString = reader.GetString()) is not null)
                    {
                        var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
                        var diff = date - DateTime.Now;
                        writer.WriteNumber(supportEnds, diff.Days);
                    }
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
            else if (IsMatchProperty(reader, releases))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                {
                    writer.WritePropertyName(releases);
                    writer.WriteStartArray();
                    ReadReleaseObject(ref reader, writer);
                    writer.WriteEndArray();
                }
            }

        }

        writer.WriteEndObject();
        ArrayPool<byte>.Shared.Return(rentedArray);
    }

    private static void ReadReleaseObject(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        var releaseDate = "release-date"u8;
        var releaseVersion = "release-version"u8;
        var security = "security"u8;
        var cveList = "cve-list"u8;
        var trueValue = "true"u8;
        bool inCveList = false;
        bool isSecurity = false;
        int depth = reader.CurrentDepth + 1;
        int skipDepth = depth + 1;
        int count = 0;
        
        while (reader.Read())
        {
            if (reader.CurrentDepth > skipDepth)
            {
                continue;
            }
            
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                if (reader.CurrentDepth == depth)
                {
                    writer.WriteStartObject();
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (reader.CurrentDepth == depth)
                {
                    writer.WriteEndObject();
                }
            }
            else if (reader.CurrentDepth <= depth && reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }
            else if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }
            else if (IsProperty(reader)  && 
                (IsMatchName(reader, releaseDate) || IsMatchName(reader, releaseVersion)) )
            {
                writer.WritePropertyName(reader.ValueSpan);
                Console.Write(Encoding.UTF8.GetString(reader.ValueSpan));
                Console.Write(": ");
                reader.Read();
                writer.WriteStringValue(reader.ValueSpan);
                Console.WriteLine(Encoding.UTF8.GetString(reader.ValueSpan));
            }
            else if (IsMatchProperty(reader, cveList))
            {
                writer.WritePropertyName(reader.ValueSpan);
                ProcessCveList(ref reader, writer);
                count++;
            }
            else if (IsMatchProperty(reader, security))
            {
                writer.WritePropertyName(reader.ValueSpan);
                reader.Read();
                var truth = reader.GetBoolean();
                writer.WriteBooleanValue(truth);
                isSecurity = truth;
            }
        }
    }

    private static void ProcessCveList(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        bool inList = true;
        while(inList && reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    inList = false;
                    break;
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;
                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.ValueSpan);
                    break;
                case JsonTokenType.String:
                    writer.WriteStringValue(reader.ValueSpan);
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    private static bool IsMatchProperty(Utf8JsonReader reader, ReadOnlySpan<byte> name) => 
       IsProperty(reader) && IsMatchName(reader, name);

    private static bool IsMatchName(Utf8JsonReader reader, ReadOnlySpan<byte> name) => 
        reader.ValueSpan.SequenceCompareTo(name) is 0;

    private static bool IsProperty(Utf8JsonReader reader) => reader.TokenType is JsonTokenType.PropertyName;
    public static List<string> GetUrls(Stream stream)
    {
        ReadOnlySpan<byte> releaseJson = "releases.json"u8;
        List<string> urls= new();
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(SIZE);
        Span<byte> buffer = rentedArray;
        int read = stream.Read(buffer);
        ReadOnlySpan<byte> text = buffer.Slice(0, read);
        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);
        var json = new Json(stream, ref reader, null, buffer, text);

        while (json.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName && reader.ValueSpan.SequenceCompareTo(releaseJson) is 0)
            {
                if (json.Read())
                {
                    var val = reader.GetString() ?? "blah";
                    urls.Add(val);
                }
            }
        }

        ArrayPool<byte>.Shared.Return(rentedArray);
        return urls;
    }
    private static void UpdateReader(Stream stream, ref ReadOnlySpan<byte> text, ref Span<byte> buffer, ref Utf8JsonReader reader)
    {
        text = text.Slice((int)reader.BytesConsumed);
        int leftoverLength = text.Length;
        text.CopyTo(buffer);
        int read = stream.Read(buffer.Slice(leftoverLength));
        text = buffer.Slice(0, read + leftoverLength);
        reader = new Utf8JsonReader(buffer, isFinalBlock: read is 0, reader.CurrentState);
    }
}
