using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReleaseJson;

public ref struct Json(Stream stream, ref Utf8JsonReader utf8JsonReader, Utf8JsonWriter? utf8JsonWriter, Span<byte> buffer, ReadOnlySpan<byte> text)
{
    public Stream Stream { get; init; } = stream;
    public Utf8JsonReader Reader = utf8JsonReader;
    public Utf8JsonWriter? Writer {get ; init; } = utf8JsonWriter;
    public Span<byte> Buffer {get; init;} = buffer;
    public ReadOnlySpan<byte> Text {get; set;} = text;

    public bool IsValue(ReadOnlySpan<byte> value) => Reader.ValueTextEquals(value);

    public bool IsProperty() => Reader.TokenType is JsonTokenType.PropertyName;

    public bool IsPropertyValue(ReadOnlySpan<byte> value) => IsProperty() && IsValue(value);

    public void WriteProperty()
    {
        Writer.WritePropertyName(Reader.ValueSpan);
        if (Reader.Read() || UpdateReader())
        {
            Writer.WriteStringValue(Reader.ValueSpan);
            // Console.WriteLine(Reader.GetString());
        } 
    }

    public bool ReadUpdate() => Reader.Read() || UpdateReader();

    public bool UpdateReader()
    {
        Text = Text.Slice((int)Reader.BytesConsumed);
        int leftoverLength = Text.Length;
        Text.CopyTo(Buffer);
        int read = stream.Read(Buffer.Slice(leftoverLength));

        if (read is 0)
        {
            return false;
        }

        int length = read + leftoverLength;
        Text = Buffer.Slice(0, length);
        var final = read < 4096 - leftoverLength;
        Reader = new Utf8JsonReader(Buffer, isFinalBlock: final, Reader.CurrentState);
        return true;
    }
}
