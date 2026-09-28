# Source pull probe

This standalone probe asks whether a closed Source query can drive scanning,
projection, encoding, buffering, and cancellation without first materializing
the complete line population.

It is deliberately outside the dotnet-inspect product graph. It does not
define a production architecture or recreate House, QuerySpace, or the retired
QueryOverflow substrate.

The result supports a focused production design for a cold pull path, but not
a universal replacement for complete materialization. Browser measurements
show that the host should select the path using both the closed query shape and
an input-size hint. See [the measured evidence](results.md).

## Variants

- **Complete** scans and materializes every line before applying the terminal,
  then retains the complete encoded output.
- **Oracle** is a specialized direct loop that scans only until the closed
  answer is known and sends one encoded frame at a time to a checksum sink.
- **Pull** exposes the same specialized execution through a cold `Stream`.
  Construction scans nothing and allocates no frame. The first read starts
  execution, each read grants bounded delivery credit, and disposal stops
  production.

All variants produce byte-identical NDJSON frames:

<!-- markdownlint-disable MD013 -->

```json
{"kind":"row","number":1,"text":"public static string Value0 => \u0022the selected value\u0022;"}
{"kind":"complete"}
```

<!-- markdownlint-enable MD013 -->

`Count` emits only a terminal frame with its value. An over-limit line emits a
terminal failure frame instead of a success-shaped empty result.

The framing is experimental. It makes partial rows, completion, and failure
observable without requiring one complete JSON byte array.

## Closed plans

- complete `Rows`;
- `Count`;
- `Head(10)`; and
- case-insensitive `Contains("the")` followed by `Head(10)`.

The corpora are deterministic 1-, 10-, 100-, and 10,000-line source-shaped
documents plus the existing Clarissa Harlowe volume used by the word-count
benchmarks.

## Run

Use the repository-selected SDK and run from the repository root:

```bash
dotnet run sourcepull/sourcepull.cs -- verify
dotnet run sourcepull/sourcepull.cs -- demo
dotnet run sourcepull/sourcepull.cs -- measure 21
dotnet run sourcepull/sourcepull.cs -- measure 21 Lines10000
```

Publish the same file-based app with NativeAOT:

```bash
dotnet publish sourcepull/sourcepull.cs -c Release -r linux-x64 \
  -p:PublishAot=true -o artifacts/sourcepull/native
./artifacts/sourcepull/native/sourcepull measure 21
```

Build or run the Browser/Wasm host:

```bash
dotnet build sourcepull/browser/SourcePull.Browser.csproj -c Release
dotnet run --project sourcepull/browser/SourcePull.Browser.csproj -c Release
```

The page checks cold construction, byte identity, scan counts, emitted rows,
and pull counts before reporting measurements. It compares the three managed
kernel variants with a WHATWG `ReadableStream` over the same 4-KiB managed
delivery buffer.

Publish interpreter and Mono AOT payloads separately:

```bash
dotnet publish sourcepull/browser/SourcePull.Browser.csproj -c Release \
  -p:RunAOTCompilation=false -o artifacts/sourcepull/browser-interpreter
dotnet publish sourcepull/browser/SourcePull.Browser.csproj -c Release \
  -p:RunAOTCompilation=true -o artifacts/sourcepull/browser-aot
```

Serve the selected output's `wwwroot` directory over HTTP and open
`index.html`. Published framework assets include extensions such as `.dat` and
`.wasm`; the static host must serve them rather than return 404.

`verify` checks byte equivalence across reader buffers of 1, 7, 256, and 4096
bytes. It also pins cold start, `Head` early stop, reader cancellation, bounded
frame retention, and visible over-line failure.

`measure` rotates the three variants and reports median elapsed time,
allocation, oracle ratio, lines scanned, rows materialized, rows emitted, pull
count, peak frame capacity, and output bytes.

## Decision rule

The pull model earns a Browser/Wasm probe only if:

- complete `Rows` remains close to the specialized oracle;
- bounded `Head` avoids the unrequested tail and materially repays its stream
  overhead on large documents;
- small-document costs remain acceptable in absolute terms;
- retained bytes stay bounded by the reader buffer and one encoded frame; and
- the same output and failure state is observed for every compared variant.

The experiment does not claim incremental acquisition or checksum settlement.
It begins with an already settled decoded document.

The probe passed those criteria. The remaining production question is not
whether the pull kernel can be efficient; it is how a House host communicates
settled input size and closed terminal demand so that small inputs keep the
lower-overhead complete path while large or selective requests use cold pull.
