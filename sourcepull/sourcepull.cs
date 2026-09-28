using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

#if !SOURCE_PULL_BROWSER
return SourcePullApp.Run(args);

static class SourcePullApp
{
    public static int Run(string[] args)
    {
        string command = args.FirstOrDefault() ?? "verify";

        return command switch
        {
            "verify" => Verify(),
            "measure" => Measure(args),
            "demo" => Demo(),
            _ => Usage()
        };
    }

    private static int Verify()
    {
        Corpus[] corpora = CorpusCatalog.Load();
        ClosedSourcePlan[] plans = ClosedSourcePlan.All;
        int comparisons = 0;

        foreach (Corpus corpus in corpora)
        {
            foreach (ClosedSourcePlan plan in plans)
            {
                RunResult complete = SourceExecutions.Complete(corpus.Text, plan);
                RunResult oracle = SourceExecutions.Oracle(corpus.Text, plan);

                AssertEquivalent(corpus, plan, complete, oracle, "oracle");

                foreach (int bufferSize in new[] { 1, 7, 256, 4096 })
                {
                    RunResult pull = SourceExecutions.Pull(corpus.Text, plan, bufferSize);
                    AssertEquivalent(corpus, plan, complete, pull, $"pull/{bufferSize}");
                    comparisons++;
                }
            }
        }

        VerifyColdStart();
        VerifyEarlyStop();
        VerifyCancellation();
        VerifyVisibleFailure();

        Console.WriteLine($"PASS: {comparisons} byte-equivalence comparisons");
        Console.WriteLine("PASS: cold start, early stop, cancellation, and visible failure");
        return 0;
    }

    private static int Measure(string[] args)
    {
        int iterations = args.Length > 1 && int.TryParse(args[1], out int parsed)
            ? parsed
            : 21;
        string? corpusFilter = args.Length > 2 ? args[2] : null;

        if (iterations < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Use at least three iterations.");
        }

        Corpus[] corpora = CorpusCatalog.Load()
            .Where(corpus => corpusFilter is null ||
                corpus.Name.Equals(corpusFilter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (corpora.Length is 0)
        {
            throw new ArgumentException($"Unknown corpus '{corpusFilter}'.");
        }

        Console.WriteLine(
            "Corpus,Plan,Variant,MedianUs,MedianAllocatedBytes,RatioToOracle," +
            "LinesScanned,RowsMaterialized,RowsEmitted,Pulls,PeakFrameBytes,OutputBytes");

        foreach (Corpus corpus in corpora)
        {
            foreach (ClosedSourcePlan plan in ClosedSourcePlan.All)
            {
                Variant[] variants =
                [
                    new("Complete", () => SourceExecutions.Complete(corpus.Text, plan)),
                    new("Oracle", () => SourceExecutions.Oracle(corpus.Text, plan)),
                    new("Pull", () => SourceExecutions.Pull(corpus.Text, plan, 4096))
                ];

                foreach (Variant variant in variants)
                {
                    _ = variant.Run();
                }

                Dictionary<string, List<Sample>> samples = variants.ToDictionary(
                    variant => variant.Name,
                    _ => new List<Sample>(iterations));

                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    int offset = iteration % variants.Length;

                    for (int index = 0; index < variants.Length; index++)
                    {
                        Variant variant = variants[(index + offset) % variants.Length];
                        samples[variant.Name].Add(TakeSample(variant.Run));
                    }
                }

                long oracleTicks = Median(samples["Oracle"].Select(sample => sample.ElapsedTicks));

                foreach (Variant variant in variants)
                {
                    List<Sample> values = samples[variant.Name];
                    long ticks = Median(values.Select(sample => sample.ElapsedTicks));
                    long allocated = Median(values.Select(sample => sample.AllocatedBytes));
                    RunResult result = values[^1].Result;
                    double microseconds = ticks * 1_000_000d / Stopwatch.Frequency;
                    double ratio = ticks / (double)oracleTicks;
                    ExecutionMetrics metrics = result.Metrics;

                    Console.WriteLine(
                        $"{corpus.Name},{plan.Name},{variant.Name},{microseconds:F3}," +
                        $"{allocated},{ratio:F3},{metrics.LinesScanned}," +
                        $"{metrics.RowsMaterialized},{metrics.RowsEmitted}," +
                        $"{metrics.PullCount},{metrics.PeakFrameBytes},{result.ByteCount}");
                }
            }
        }

        return 0;
    }

    private static int Demo()
    {
        string text = CorpusCatalog.CreateSourceLikeDocument(20);
        ClosedSourcePlan plan = ClosedSourcePlan.FilteredHead10;
        using var stream = new ColdSourceStream(text, plan);
        byte[] buffer = new byte[37];

        Console.WriteLine($"Started before read: {stream.Metrics.Started}");

        while (true)
        {
            int read = stream.Read(buffer);

            if (read is 0)
            {
                break;
            }

            Console.Write(Encoding.UTF8.GetString(buffer.AsSpan(0, read)));
        }

        Console.WriteLine($"Started after read: {stream.Metrics.Started}");
        Console.WriteLine(
            $"Scanned={stream.Metrics.LinesScanned}; " +
            $"Emitted={stream.Metrics.RowsEmitted}; Pulls={stream.Metrics.PullCount}");
        return 0;
    }

    private static Sample TakeSample(Func<RunResult> run)
    {
        long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        RunResult result = run();
        long elapsed = Stopwatch.GetTimestamp() - started;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
        GC.KeepAlive(result);
        return new(elapsed, allocated, result);
    }

    private static long Median(IEnumerable<long> values)
    {
        long[] ordered = values.Order().ToArray();
        return ordered[ordered.Length / 2];
    }

    private static void VerifyColdStart()
    {
        using var stream = new ColdSourceStream(
            CorpusCatalog.CreateSourceLikeDocument(100),
            ClosedSourcePlan.Rows);

        Assert(!stream.Metrics.Started, "A newly opened pull stream must be cold.");
        Assert(stream.Metrics.LinesScanned is 0, "A cold stream must not scan lines.");
        Assert(stream.Metrics.PeakFrameBytes is 0, "A cold stream must not allocate a frame.");

        Span<byte> oneByte = stackalloc byte[1];
        Assert(stream.Read(oneByte) is 1, "The first pull should produce data.");
        Assert(stream.Metrics.Started, "The first pull must start execution.");
    }

    private static void VerifyEarlyStop()
    {
        const int documentLines = 10_000;
        string text = CorpusCatalog.CreateSourceLikeDocument(documentLines);
        RunResult head = SourceExecutions.Pull(text, ClosedSourcePlan.Head10, 4096);
        RunResult filtered = SourceExecutions.Pull(text, ClosedSourcePlan.FilteredHead10, 4096);

        Assert(head.Metrics.LinesScanned is 10, "Head(10) must scan exactly ten matching lines.");
        Assert(
            filtered.Metrics.LinesScanned < documentLines,
            "Filtered Head(10) must stop before the unrequested tail.");
        Assert(filtered.Metrics.RowsEmitted is 10, "Filtered Head(10) must emit ten rows.");
    }

    private static void VerifyCancellation()
    {
        using var stream = new ColdSourceStream(
            CorpusCatalog.CreateSourceLikeDocument(10_000),
            ClosedSourcePlan.Rows);
        Span<byte> oneByte = stackalloc byte[1];

        Assert(stream.Read(oneByte) is 1, "The cancellation probe should start one frame.");
        Assert(stream.Metrics.LinesScanned is 1, "One-byte credit must not scan a second row.");
        stream.Dispose();
        Assert(stream.Metrics.LinesScanned is 1, "Disposal must not continue production.");
    }

    private static void VerifyVisibleFailure()
    {
        string oversized = new('x', SourceLimits.MaxLineChars + 1);

        VerifyFailureEquivalent("oversized-first", oversized);
        VerifyFailureEquivalent(
            "oversized-after-prefix",
            CorpusCatalog.CreateSourceLikeDocument(3) + oversized);

        VerifyUnrequestedFailureTail(ClosedSourcePlan.Head10, 10, oversized);
        VerifyUnrequestedFailureTail(ClosedSourcePlan.FilteredHead10, 64, oversized);
    }

    private static void VerifyUnrequestedFailureTail(
        ClosedSourcePlan plan,
        int answerLineCount,
        string oversized)
    {
        string text =
            CorpusCatalog.CreateSourceLikeDocument(answerLineCount) + oversized;
        RunResult complete = SourceExecutions.Complete(text, plan);
        RunResult oracle = SourceExecutions.Oracle(text, plan);
        RunResult pull = SourceExecutions.Pull(text, plan, 7);

        Assert(!complete.Metrics.Failed, $"{plan.Name} must not observe an unrequested tail.");
        AssertEquivalent(
            new("oversized-unrequested-tail", text),
            plan,
            complete,
            oracle,
            "oracle");
        AssertEquivalent(
            new("oversized-unrequested-tail", text),
            plan,
            complete,
            pull,
            "pull");
    }

    private static void VerifyFailureEquivalent(string name, string text)
    {
        foreach (ClosedSourcePlan plan in ClosedSourcePlan.All)
        {
            RunResult complete = SourceExecutions.Complete(text, plan);
            RunResult oracle = SourceExecutions.Oracle(text, plan);
            RunResult pull = SourceExecutions.Pull(text, plan, 7);

            Assert(complete.Metrics.Failed, $"{plan.Name} must expose an oversized line.");
            AssertEquivalent(new(name, text), plan, complete, oracle, "oracle");
            AssertEquivalent(new(name, text), plan, complete, pull, "pull");
        }
    }

    private static void AssertEquivalent(
        Corpus corpus,
        ClosedSourcePlan plan,
        RunResult expected,
        RunResult actual,
        string variant)
    {
        Assert(
            expected.ByteCount == actual.ByteCount &&
            expected.Checksum == actual.Checksum &&
            expected.Metrics.Failed == actual.Metrics.Failed,
            $"{corpus.Name}/{plan.Name}/{variant} differs from complete output. " +
            $"Expected {expected.ByteCount}/{expected.Checksum:x16}; " +
            $"actual {actual.ByteCount}/{actual.Checksum:x16}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: dotnet run sourcepull/sourcepull.cs -- [verify|demo|measure [iterations] [corpus]]");
        return 2;
    }

    private sealed record Variant(string Name, Func<RunResult> Run);

    private sealed record Sample(long ElapsedTicks, long AllocatedBytes, RunResult Result);
}
#endif

static class SourceExecutions
{
    public static RunResult Complete(string text, ClosedSourcePlan plan)
    {
        var metrics = new ExecutionMetrics { Started = true };
        var cursor = new LineCursor(text);
        var rows = new List<SourceRow>();
        string? failure = null;

        while (cursor.TryRead(out int number, out ReadOnlySpan<char> line))
        {
            metrics.LinesScanned++;

            if (line.Length > SourceLimits.MaxLineChars)
            {
                failure = SourceLimits.LineTooLongMessage;
                break;
            }

            rows.Add(new(number, line.ToString()));
            metrics.RowsMaterialized++;
        }

        var sink = new CompleteBufferSink();
        using var encoder = new FrameEncoder(metrics);

        switch (plan.Terminal)
        {
            case SourceTerminal.Rows:
                foreach (SourceRow row in rows)
                {
                    if (plan.Matches(row.Text))
                    {
                        WriteRow(encoder, sink, metrics, row.Number, row.Text);
                    }
                }

                WriteCompletionOrFailure(encoder, sink, metrics, plan, failure);
                break;

            case SourceTerminal.Head:
                foreach (SourceRow row in rows)
                {
                    if (!plan.Matches(row.Text))
                    {
                        continue;
                    }

                    WriteRow(encoder, sink, metrics, row.Number, row.Text);

                    if (metrics.RowsEmitted == plan.HeadCount)
                    {
                        break;
                    }
                }

                WriteCompletionOrFailure(
                    encoder,
                    sink,
                    metrics,
                    plan,
                    metrics.RowsEmitted == plan.HeadCount ? null : failure);
                break;

            case SourceTerminal.Count:
                if (failure is null)
                {
                    metrics.ResultCount = rows.LongCount(row => plan.Matches(row.Text));
                }

                WriteCompletionOrFailure(encoder, sink, metrics, plan, failure);
                break;
        }

        return sink.ToResult(metrics);
    }

    public static RunResult Oracle(string text, ClosedSourcePlan plan)
    {
        var metrics = new ExecutionMetrics { Started = true };
        var sink = new ChecksumSink();
        using var encoder = new FrameEncoder(metrics);
        var cursor = new LineCursor(text);

        if (plan.Terminal is SourceTerminal.Count)
        {
            while (TryReadValidated(cursor, metrics, encoder, sink, out _, out ReadOnlySpan<char> line))
            {
                if (plan.Matches(line))
                {
                    metrics.ResultCount++;
                }
            }

            if (!metrics.Failed)
            {
                sink.Write(encoder.EncodeComplete(plan, metrics.ResultCount).Span);
            }

            return sink.ToResult(metrics);
        }

        while (TryReadValidated(cursor, metrics, encoder, sink, out int number, out ReadOnlySpan<char> line))
        {
            if (!plan.Matches(line))
            {
                continue;
            }

            WriteRow(encoder, sink, metrics, number, line);

            if (plan.Terminal is SourceTerminal.Head &&
                metrics.RowsEmitted == plan.HeadCount)
            {
                break;
            }
        }

        if (!metrics.Failed)
        {
            sink.Write(encoder.EncodeComplete(plan, metrics.ResultCount).Span);
        }

        return sink.ToResult(metrics);
    }

    public static RunResult Pull(string text, ClosedSourcePlan plan, int readerBufferSize)
    {
        using var stream = new ColdSourceStream(text, plan);
        var sink = new ChecksumSink();
        byte[] readerBuffer = new byte[readerBufferSize];
        stream.Metrics.PeakReaderBufferBytes = readerBuffer.Length;

        while (true)
        {
            int read = stream.Read(readerBuffer);

            if (read is 0)
            {
                break;
            }

            sink.Write(readerBuffer.AsSpan(0, read));
        }

        return sink.ToResult(stream.Metrics);
    }

    private static bool TryReadValidated(
        LineCursor cursor,
        ExecutionMetrics metrics,
        FrameEncoder encoder,
        IFrameSink sink,
        out int number,
        out ReadOnlySpan<char> line)
    {
        if (!cursor.TryRead(out number, out line))
        {
            return false;
        }

        metrics.LinesScanned++;

        if (line.Length <= SourceLimits.MaxLineChars)
        {
            return true;
        }

        metrics.Failed = true;
        sink.Write(encoder.EncodeFailure(SourceLimits.LineTooLongMessage).Span);
        return false;
    }

    private static void WriteRow(
        FrameEncoder encoder,
        IFrameSink sink,
        ExecutionMetrics metrics,
        int number,
        ReadOnlySpan<char> text)
    {
        sink.Write(encoder.EncodeRow(number, text).Span);
        metrics.RowsEmitted++;
    }

    private static void WriteCompletionOrFailure(
        FrameEncoder encoder,
        IFrameSink sink,
        ExecutionMetrics metrics,
        ClosedSourcePlan plan,
        string? failure)
    {
        if (failure is null)
        {
            sink.Write(encoder.EncodeComplete(plan, metrics.ResultCount).Span);
            return;
        }

        metrics.Failed = true;
        sink.Write(encoder.EncodeFailure(failure).Span);
    }
}

sealed class ColdSourceStream : Stream
{
    private readonly ClosedSourcePlan _plan;
    private readonly LineCursor _cursor;
    private FrameEncoder? _encoder;
    private ReadOnlyMemory<byte> _pending;
    private int _pendingOffset;
    private bool _terminalFrameProduced;
    private bool _stopAfterPendingRow;
    private bool _disposed;

    public ColdSourceStream(string text, ClosedSourcePlan plan)
    {
        _plan = plan;
        _cursor = new(text);
    }

    public ExecutionMetrics Metrics { get; } = new();

    public override bool CanRead => !_disposed;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (destination.Length is 0)
        {
            return 0;
        }

        Metrics.Started = true;
        Metrics.PullCount++;
        int written = 0;

        while (destination.Length > 0)
        {
            if (_pendingOffset < _pending.Length)
            {
                int copyLength = int.Min(destination.Length, _pending.Length - _pendingOffset);
                _pending.Span.Slice(_pendingOffset, copyLength).CopyTo(destination);
                _pendingOffset += copyLength;
                destination = destination[copyLength..];
                written += copyLength;

                if (_pendingOffset < _pending.Length)
                {
                    break;
                }

                _pending = default;
                _pendingOffset = 0;
                continue;
            }

            if (_terminalFrameProduced)
            {
                break;
            }

            ProduceFrame();
        }

        return written;
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Read(buffer.Span));
    }

    private void ProduceFrame()
    {
        _encoder ??= new FrameEncoder(Metrics);

        if (_stopAfterPendingRow)
        {
            _pending = _encoder.EncodeComplete(_plan, Metrics.ResultCount);
            _terminalFrameProduced = true;
            return;
        }

        if (_plan.Terminal is SourceTerminal.Count)
        {
            ProduceCount();
            return;
        }

        while (_cursor.TryRead(out int number, out ReadOnlySpan<char> line))
        {
            Metrics.LinesScanned++;

            if (line.Length > SourceLimits.MaxLineChars)
            {
                Metrics.Failed = true;
                _pending = _encoder.EncodeFailure(SourceLimits.LineTooLongMessage);
                _terminalFrameProduced = true;
                return;
            }

            if (!_plan.Matches(line))
            {
                continue;
            }

            _pending = _encoder.EncodeRow(number, line);
            Metrics.RowsEmitted++;

            if (_plan.Terminal is SourceTerminal.Head &&
                Metrics.RowsEmitted == _plan.HeadCount)
            {
                _stopAfterPendingRow = true;
            }

            return;
        }

        _pending = _encoder.EncodeComplete(_plan, Metrics.ResultCount);
        _terminalFrameProduced = true;
    }

    private void ProduceCount()
    {
        while (_cursor.TryRead(out _, out ReadOnlySpan<char> line))
        {
            Metrics.LinesScanned++;

            if (line.Length > SourceLimits.MaxLineChars)
            {
                Metrics.Failed = true;
                _pending = _encoder!.EncodeFailure(SourceLimits.LineTooLongMessage);
                _terminalFrameProduced = true;
                return;
            }

            if (_plan.Matches(line))
            {
                Metrics.ResultCount++;
            }
        }

        _pending = _encoder!.EncodeComplete(_plan, Metrics.ResultCount);
        _terminalFrameProduced = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _encoder?.Dispose();
        }

        base.Dispose(disposing);
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

sealed class FrameEncoder : IDisposable
{
    private readonly ExecutionMetrics _metrics;
    private readonly ArrayBufferWriter<byte> _buffer = new(256);
    private readonly Utf8JsonWriter _writer;
    private bool _used;

    public FrameEncoder(ExecutionMetrics metrics)
    {
        _metrics = metrics;
        _writer = new(_buffer);
    }

    public ReadOnlyMemory<byte> EncodeRow(int number, ReadOnlySpan<char> text)
    {
        BeginFrame();
        _writer.WriteStartObject();
        _writer.WriteString("kind"u8, "row"u8);
        _writer.WriteNumber("number"u8, number);
        _writer.WriteString("text"u8, text);
        _writer.WriteEndObject();
        return EndFrame();
    }

    public ReadOnlyMemory<byte> EncodeComplete(ClosedSourcePlan plan, long count)
    {
        BeginFrame();
        _writer.WriteStartObject();
        _writer.WriteString("kind"u8, "complete"u8);

        if (plan.Terminal is SourceTerminal.Count)
        {
            _writer.WriteNumber("count"u8, count);
        }

        _writer.WriteEndObject();
        return EndFrame();
    }

    public ReadOnlyMemory<byte> EncodeFailure(string message)
    {
        BeginFrame();
        _writer.WriteStartObject();
        _writer.WriteString("kind"u8, "failure"u8);
        _writer.WriteString("message"u8, message);
        _writer.WriteEndObject();
        return EndFrame();
    }

    private void BeginFrame()
    {
        if (_used)
        {
            _buffer.Clear();
            _writer.Reset(_buffer);
        }

        _used = true;
    }

    private ReadOnlyMemory<byte> EndFrame()
    {
        _writer.Flush();
        Span<byte> newline = _buffer.GetSpan(1);
        newline[0] = (byte)'\n';
        _buffer.Advance(1);
        _metrics.FramesEncoded++;
        _metrics.BytesEncoded += _buffer.WrittenCount;
        _metrics.PeakFrameBytes = int.Max(_metrics.PeakFrameBytes, _buffer.Capacity);
        return _buffer.WrittenMemory;
    }

    public void Dispose() => _writer.Dispose();
}

sealed class LineCursor(string text)
{
    private readonly string _text = text;
    private int _position;
    private int _number;

    public bool TryRead(out int number, out ReadOnlySpan<char> line)
    {
        if (_position >= _text.Length)
        {
            number = default;
            line = default;
            return false;
        }

        ReadOnlySpan<char> remaining = _text.AsSpan(_position);
        int newline = remaining.IndexOf('\n');
        int length = newline >= 0 ? newline : remaining.Length;

        if (length > 0 && remaining[length - 1] is '\r')
        {
            length--;
        }

        line = remaining[..length];
        number = ++_number;
        _position += newline >= 0 ? newline + 1 : remaining.Length;
        return true;
    }
}

sealed record ClosedSourcePlan(
    string Name,
    SourceTerminal Terminal,
    int HeadCount = 0,
    string? Contains = null)
{
    public static ClosedSourcePlan Rows { get; } = new("Rows", SourceTerminal.Rows);

    public static ClosedSourcePlan Count { get; } = new("Count", SourceTerminal.Count);

    public static ClosedSourcePlan Head10 { get; } = new("Head10", SourceTerminal.Head, 10);

    public static ClosedSourcePlan FilteredHead10 { get; } =
        new("FilteredHead10", SourceTerminal.Head, 10, "the");

    public static ClosedSourcePlan[] All { get; } =
        [Rows, Count, Head10, FilteredHead10];

    public bool Matches(ReadOnlySpan<char> text) =>
        Contains is null || text.Contains(Contains, StringComparison.OrdinalIgnoreCase);

    public bool Matches(string text) => Matches(text.AsSpan());
}

enum SourceTerminal
{
    Rows,
    Count,
    Head
}

sealed record Corpus(string Name, string Text);

static class CorpusCatalog
{
    public static Corpus[] Load()
    {
        string clarissaPath = Path.Combine(
            Environment.CurrentDirectory,
            "wordcount",
            "Clarissa_Harlowe",
            "clarissa_volume1.txt");

        if (!File.Exists(clarissaPath))
        {
            throw new FileNotFoundException(
                "Run the probe from the convenience repository root.",
                clarissaPath);
        }

        return
        [
            new("Lines1", CreateSourceLikeDocument(1)),
            new("Lines10", CreateSourceLikeDocument(10)),
            new("Lines100", CreateSourceLikeDocument(100)),
            new("Lines10000", CreateSourceLikeDocument(10_000)),
            new("Clarissa", File.ReadAllText(clarissaPath))
        ];
    }

    public static string CreateSourceLikeDocument(int lineCount)
    {
        var builder = new StringBuilder(lineCount * 80);

        for (int index = 0; index < lineCount; index++)
        {
            builder.Append("public static string Value");
            builder.Append(index);
            builder.Append(" => \"");
            builder.Append(index % 7 is 0 ? "the selected value" : "ordinary value");
            builder.AppendLine("\";");
        }

        return builder.ToString();
    }
}

sealed class ExecutionMetrics
{
    public bool Started { get; set; }

    public bool Failed { get; set; }

    public long LinesScanned { get; set; }

    public long RowsMaterialized { get; set; }

    public long RowsEmitted { get; set; }

    public long ResultCount { get; set; }

    public long FramesEncoded { get; set; }

    public long BytesEncoded { get; set; }

    public long PullCount { get; set; }

    public int PeakFrameBytes { get; set; }

    public int PeakReaderBufferBytes { get; set; }
}

readonly record struct SourceRow(int Number, string Text);

readonly record struct RunResult(ulong Checksum, long ByteCount, ExecutionMetrics Metrics);

interface IFrameSink
{
    void Write(ReadOnlySpan<byte> bytes);
}

sealed class ChecksumSink : IFrameSink
{
    private readonly ChecksumAccumulator _checksum = new();

    public void Write(ReadOnlySpan<byte> bytes) => _checksum.Append(bytes);

    public RunResult ToResult(ExecutionMetrics metrics) =>
        new(_checksum.Value, _checksum.Length, metrics);
}

sealed class CompleteBufferSink : IFrameSink
{
    private readonly ArrayBufferWriter<byte> _buffer = new();

    public void Write(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(_buffer.GetSpan(bytes.Length));
        _buffer.Advance(bytes.Length);
    }

    public RunResult ToResult(ExecutionMetrics metrics)
    {
        var checksum = new ChecksumAccumulator();
        checksum.Append(_buffer.WrittenSpan);
        return new(checksum.Value, checksum.Length, metrics);
    }
}

sealed class ChecksumAccumulator
{
    private const ulong OffsetBasis = 14695981039346656037;
    private const ulong Prime = 1099511628211;
    private ulong _value = OffsetBasis;

    public ulong Value => _value;

    public long Length { get; private set; }

    public void Append(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            _value ^= value;
            _value *= Prime;
        }

        Length += bytes.Length;
    }
}

static class SourceLimits
{
    public const int MaxLineChars = 64 * 1024;

    public const string LineTooLongMessage = "Source line exceeds the 65536 UTF-16 character limit.";
}
