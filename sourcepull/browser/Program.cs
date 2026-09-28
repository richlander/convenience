using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;

Console.WriteLine("Source pull Browser/Wasm probe ready.");

partial class SourcePullBrowser
{
    private const double MinimumSampleMilliseconds = 20;
    private const int MaximumBatchSize = 1_000;
    private static readonly Dictionary<int, BrowserSession> Sessions = [];
    private static readonly Dictionary<int, string> Documents = [];
    private static int _nextHandle;

    [JSExport]
    internal static int Open(int lineCount, int planIndex, int bufferSize)
    {
        string text = GetDocument(lineCount);
        ClosedSourcePlan plan = GetPlan(planIndex);
        int handle = ++_nextHandle;
        Sessions.Add(handle, new(new(text, plan), new byte[bufferSize]));
        return handle;
    }

    [JSExport]
    [return: JSMarshalAs<JSType.MemoryView>]
    internal static ArraySegment<byte> GetBuffer(int handle) =>
        GetSession(handle).Buffer;

    [JSExport]
    internal static int Pull(int handle)
    {
        BrowserSession session = GetSession(handle);
        int read = session.Stream.Read(session.Buffer);
        session.Checksum.Append(session.Buffer.AsSpan(0, read));
        return read;
    }

    [JSExport]
    internal static bool IsStarted(int handle) =>
        GetSession(handle).Stream.Metrics.Started;

    [JSExport]
    internal static double GetLinesScanned(int handle) =>
        GetSession(handle).Stream.Metrics.LinesScanned;

    [JSExport]
    internal static double GetRowsEmitted(int handle) =>
        GetSession(handle).Stream.Metrics.RowsEmitted;

    [JSExport]
    internal static double GetPullCount(int handle) =>
        GetSession(handle).Stream.Metrics.PullCount;

    [JSExport]
    internal static string GetChecksum(int handle) =>
        GetSession(handle).Checksum.Value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);

    [JSExport]
    internal static double GetInputUtf8Bytes(int lineCount) =>
        System.Text.Encoding.UTF8.GetByteCount(GetDocument(lineCount));

    [JSExport]
    internal static void Close(int handle)
    {
        if (Sessions.Remove(handle, out BrowserSession? session))
        {
            session.Stream.Dispose();
        }
    }

    [JSExport]
    internal static string MeasureKernel(
        int lineCount,
        int planIndex,
        int variantIndex,
        int iterations)
    {
        string text = GetDocument(lineCount);
        ClosedSourcePlan plan = GetPlan(planIndex);
        Func<RunResult> run = variantIndex switch
        {
            0 => () => SourceExecutions.Complete(text, plan),
            1 => () => SourceExecutions.Oracle(text, plan),
            2 => () => SourceExecutions.Pull(text, plan, 4096),
            _ => throw new ArgumentOutOfRangeException(nameof(variantIndex))
        };

        _ = run();
        int repetitions = Calibrate(run);
        long[] samples = new long[iterations];
        RunResult last = default;

        for (int index = 0; index < samples.Length; index++)
        {
            long started = Stopwatch.GetTimestamp();

            for (int repetition = 0; repetition < repetitions; repetition++)
            {
                last = run();
            }

            samples[index] = Stopwatch.GetTimestamp() - started;
            GC.KeepAlive(last);
        }

        Array.Sort(samples);
        double microseconds =
            samples[samples.Length / 2] * 1_000_000d /
            Stopwatch.Frequency /
            repetitions;
        ExecutionMetrics metrics = last.Metrics;

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{microseconds:F3},{metrics.LinesScanned},{metrics.RowsMaterialized},{metrics.RowsEmitted},{metrics.PullCount},{last.ByteCount},{last.Checksum},{repetitions}");
    }

    private static int Calibrate(Func<RunResult> run)
    {
        long shortest = long.MaxValue;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            long started = Stopwatch.GetTimestamp();
            RunResult result = run();
            long elapsed = Stopwatch.GetTimestamp() - started;
            GC.KeepAlive(result);

            if (elapsed > 0)
            {
                shortest = long.Min(shortest, elapsed);
            }
        }

        double elapsedMilliseconds = shortest is long.MaxValue
            ? 0
            : shortest * 1_000d / Stopwatch.Frequency;
        return Math.Clamp(
            (int)Math.Ceiling(MinimumSampleMilliseconds / Math.Max(elapsedMilliseconds, 0.001)),
            1,
            MaximumBatchSize);
    }

    private static string GetDocument(int lineCount)
    {
        if (!Documents.TryGetValue(lineCount, out string? text))
        {
            text = CorpusCatalog.CreateSourceLikeDocument(lineCount);
            Documents.Add(lineCount, text);
        }

        return text;
    }

    private static ClosedSourcePlan GetPlan(int planIndex) =>
        planIndex >= 0 && planIndex < ClosedSourcePlan.All.Length
            ? ClosedSourcePlan.All[planIndex]
            : throw new ArgumentOutOfRangeException(nameof(planIndex));

    private static BrowserSession GetSession(int handle) =>
        Sessions.TryGetValue(handle, out BrowserSession? session)
            ? session
            : throw new ArgumentOutOfRangeException(nameof(handle));

    private sealed record BrowserSession(ColdSourceStream Stream, byte[] Buffer)
    {
        public ChecksumAccumulator Checksum { get; } = new();
    }
}
