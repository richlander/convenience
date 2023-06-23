using System.Text.Json;

namespace ReleaseJson;

public ref struct Json(Stream stream, ref Utf8JsonReader utf8JsonReader, Utf8JsonWriter utf8JsonWriter, Span<byte> buffer, Span<byte> text)
{
    public Stream Stream { get; init; } = stream;
    public Utf8JsonReader Reader { get; set; } = utf8JsonReader;
    public Utf8JsonWriter Writer {get ; init; } = utf8JsonWriter;
    public Span<byte> Buffer {get; init;} = buffer;
    public ReadOnlySpan<byte> Text {get; set;} = text;

    public bool Read()
    {
        if (Reader.Read())
        {
            return true;
        }

        return UpdateReader();
    }

    private bool UpdateReader()
    {
        Text = Text.Slice((int)Reader.BytesConsumed);
        int leftoverLength = Text.Length;
        Text.CopyTo(Buffer);
        int read = stream.Read(Buffer.Slice(leftoverLength));
        int length = read + leftoverLength;

        if (length is 0)
        {
            return false;
        }

        Text = Buffer.Slice(0, length);
        Reader = new Utf8JsonReader(Buffer, isFinalBlock: read is 0, Reader.CurrentState);
        return true;
    }

}