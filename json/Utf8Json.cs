using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Principal;
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
        using var message = await httpClient.GetAsync(gist, HttpCompletionOption.ResponseContentRead);
        using var releases = message.Content.ReadAsStream();

        WriteHeadMatter(writer);

        foreach (var url in GetUrls(releases))
        {
            using var releaseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            using var release = releaseMessage.Content.ReadAsStream();
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
        var eol = "eol-date"u8;
        var supportEnds = "support-ends-in-days"u8;
        var releases = "releases"u8;

        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(SIZE);
        Span<byte> buffer = rentedArray;
        int read = stream.Read(buffer);
        ReadOnlySpan<byte> text = buffer.Slice(0, read);
        var reader = new Utf8JsonReader(text, isFinalBlock: false, state: default);
        var j = new Json(stream, ref reader, writer, buffer, text);

        writer.WriteStartObject();

        while (j.Reader.Read())
        {
            if (j.IsProperty() && (j.IsValue(channel) || j.IsValue(support)))
            {            
                j.WriteProperty();                                                                                                                                   
            }
            else if (j.IsPropertyValue(eol))
            {
                j.WriteProperty();
                int days = GetDaysAgo(ref j) * -1;
                writer.WriteNumber(supportEnds, days);
            }
            else if (j.IsPropertyValue(releases))
            {
                if (j.Reader.Read() && j.Reader.TokenType == JsonTokenType.StartArray)
                {
                    writer.WritePropertyName(releases);
                    writer.WriteStartArray();
                    AddReleases(ref j);
                    writer.WriteEndArray();
                }
            }

            if (j.Reader.IsFinalBlock && j.Reader.CurrentDepth is 0)
            {
                break;
            }

        }

        writer.WriteEndObject();
        ArrayPool<byte>.Shared.Return(rentedArray);
    }

    private static void AddReleases(ref Json j)
    {
        AddRelease(ref j, out bool isSecurity);

        if (isSecurity)
        {
            return;
        }

        while (true)
        {
            if (j.Reader.BytesConsumed > 4096 * .8)
            {
                j.UpdateReader();
            }

            var oldReader = j.Reader;

            if(IsReleaseSecurity(ref j, out JsonTokenType lastToken))
            {                
                j.Reader = oldReader;
                AddRelease(ref j, out _);
                return;
            }
            else if (lastToken is JsonTokenType.EndArray)
            {
                return;
            }
        }
    }

    private static bool IsReleaseSecurity(ref Json j, out JsonTokenType lastToken)
    {
        var security = "security"u8;
        int depth = j.Reader.CurrentDepth + 1;
        lastToken = JsonTokenType.None;
        bool inSecurity = false;
        bool pastSecurity = false;

        while(j.Reader.Read())
        {
            if (j.Reader.CurrentDepth <= depth)
            {
                lastToken = j.Reader.TokenType;
                
                if (lastToken is JsonTokenType.StartObject)
                {
                    depth = j.Reader.CurrentDepth;
                    continue;
                }

                return false;
            }
            else if (pastSecurity)
            {
                continue;
            }
            else if (j.IsPropertyValue(security))
            {
                inSecurity = true;
            }
            else if (inSecurity)
            {
                if (j.Reader.GetBoolean())
                {
                    return true;
                }

                inSecurity = false;
                pastSecurity = true;
            }
        }

        return false;
    }

    private static void AddRelease(ref Json j, out bool isSecurity)
    {
        var releaseDate = "release-date"u8;
        var releaseVersion = "release-version"u8;
        var security = "security"u8;
        var cveList = "cve-list"u8;
        var daysAgo = "released-days-ago"u8;
        isSecurity = false;

        Utf8JsonWriter writer = j.Writer;
        int depth = j.Reader.CurrentDepth + 1;
        bool skip = false;
        
        writer.WriteStartObject();

        while (j.Reader.Read() || j.UpdateReader())
        {

            if (j.Reader.CurrentDepth <= depth)
            {
                if (j.Reader.TokenType is JsonTokenType.StartObject)
                {
                    depth = j.Reader.CurrentDepth;
                    continue;
                }

                break;
            }
            else if (skip)
            {
                continue;
            }
            else if (j.IsValue(releaseDate))
            {
                j.WriteProperty();
                int days = GetDaysAgo(ref j);
                writer.WriteNumber(daysAgo, days);                
            }
            else if (j.IsValue(releaseVersion))
            {
                j.WriteProperty();
            }
            else if (j.IsValue(security))
            {
                j.ReadUpdate();   

                isSecurity = j.Reader.GetBoolean();
                writer.WritePropertyName(security);
                writer.WriteBooleanValue(isSecurity);
                skip = true;

                if (j.ReadUpdate() && j.IsValue(cveList))
                {
                    ProcessCveList(ref j);
                }
            }
        }

        writer.WriteEndObject();
    }

    private static void ProcessCveList(ref Json j)
    {
        var cveList = "cve-list"u8;
        bool inList = true;
        Utf8JsonWriter writer = j.Writer;

        while(inList && j.Reader.Read())
        {
            switch (j.Reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    writer.WritePropertyName(cveList);
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
                    j.WriteProperty();
                    break;
                case JsonTokenType.Null:
                    writer.WriteNull(cveList);
                    inList = false;
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    public static List<string> GetUrls(Stream stream)
    {
        ReadOnlySpan<byte> releaseJson = "releases.json"u8;
        List<string> urls= new();
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(SIZE);
        Span<byte> buffer = rentedArray;
        int read = stream.Read(buffer);
        ReadOnlySpan<byte> text = buffer.Slice(0, read);
        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);
        var j = new Json(stream, ref reader, null, buffer, text);

        while (j.Reader.Read())
        {
            if (j.IsPropertyValue(releaseJson))
            {
                if (j.Reader.Read())
                {
                    var val = j.Reader.GetString() ?? throw new Exception();
                    urls.Add(val);
                }
            }
        }

        ArrayPool<byte>.Shared.Return(rentedArray);
        return urls;
    }

    private static int GetDaysAgo(ref Json j)
    {
        string? dateString;
        if ((dateString = j.Reader.GetString()) is not null)
        {
            var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
            return (DateTime.Now - date).Days;
        }

        return 0;
    }
}
