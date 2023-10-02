using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;

namespace JsonReaders;

public class JsonPipeReader(PipeReader reader, ReadResult result)
{
    private JsonReaderState _readerState = default;
    private SequencePosition _position = default;
    private int _depth = 0;
    private long _bytesConsumed = 0;
    private readonly PipeReader _pipeReader = reader;
    private ReadOnlySequence<byte> _text = result.Buffer;

    public void UpdateState(Utf8JsonReader reader)
    {
        _bytesConsumed = reader.BytesConsumed;
        _position = reader.Position;
        _readerState = reader.CurrentState;
        _depth = reader.CurrentDepth;
    }

    public Utf8JsonReader GetReader()
    {
        var slice = _bytesConsumed > 0 ? _text.Slice(_position) : _text;
        var reader = new Utf8JsonReader(slice, false, _readerState);
        return reader;
    }

    public int Depth => _depth;

    public async Task AdvanceAsync()
    {
        _pipeReader.AdvanceTo(_position);
        var result = await _pipeReader.ReadAsync();
        _text = result.Buffer;
        _bytesConsumed = 0;
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
    public bool ReadToPropertyValue<T>(ReadOnlySpan<byte> name, [NotNullWhen(true)] out T? value, bool updateState = true)
    {
        var reader = GetReader();
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
}
