using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace JsonReaders;

public class JsonStreamReader(Stream stream, byte[] buffer, int readCount)
{
    private readonly Stream _stream = stream;
    private readonly byte[] _buffer = buffer;
    private JsonReaderState _readerState = default;
    private int _depth = 0;
    private long _bytesConsumed = 0;
    private int _readCount = readCount;

    public void UpdateState(Utf8JsonReader reader)
    {
        _bytesConsumed += reader.BytesConsumed;
        _readerState = reader.CurrentState;
        _depth = reader.CurrentDepth;
    }

    public Utf8JsonReader GetReader()
    {
        ReadOnlySpan<byte> slice = _bytesConsumed > 0 || _readCount < Size ? _buffer.AsSpan()[(int)_bytesConsumed.._readCount] : _buffer;
        var reader = new Utf8JsonReader(slice, false, _readerState);
        return reader;
    }

    public int Depth => _depth;

    public static int Size => 4 * 1024;

    public async Task AdvanceAsync()
    {
        // Save off existing text
        int leftoverLength = FlipBuffer();

        // Read from stream to fill remainder of buffer
        int read = await _stream.ReadAsync(_buffer.AsMemory()[leftoverLength..]);
        _readCount = read + leftoverLength;
    }

    private int FlipBuffer()
    {
        var buffer = _buffer.AsSpan();
        var text = buffer[(int)_bytesConsumed.._readCount];
        text.CopyTo(buffer);
        _bytesConsumed = 0;
        return text.Length;
    }

    public bool ReadToDepth(int depth, bool updateState = true)
    {
        var reader = GetReader();
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

    public bool ReadNext([NotNullWhen(true)] out JsonTokenType tokenType)
    {
        var reader = GetReader();

        if (reader.Read())
        {
            tokenType = reader.TokenType;
            return true;
        }

        tokenType = default;
        return false;
    }

    public bool ReadToTokenType(JsonTokenType tokenType, bool updateState = true)
    {
        var reader = GetReader();
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
        var reader = GetReader();
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
    public bool ReadToPropertyValue<T>(ReadOnlySpan<byte> name, [NotNullWhen(true)] out T value, bool updateState = true)
    {
        var reader = GetReader();
        var found = ReadToProperty(ref reader, name);
        value = default!;

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
}
