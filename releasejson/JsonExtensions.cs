using System.Buffers;
using System.Text.Json;

namespace JsonExtensions;

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
