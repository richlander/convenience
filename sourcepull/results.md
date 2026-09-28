# Cold Source pull evidence

## Decision

The specialized cold pull kernel is viable and merits a focused production
design. It remains close to a direct-loop oracle when complete output is
requested, avoids the unrequested tail for bounded terminals, and composes with
a real WHATWG `ReadableStream` without eager managed execution.

It should not replace complete materialization universally. Browser interop
has a fixed cost that dominates small inputs. The production host should choose
between complete and pull execution using both the closed query shape and a
settled input-size hint. This result does not justify recreating a generic
QueryOverflow execution substrate.

## Method

Measurements were captured on 2026-09-28 from base `9eca1355fd0d` with .NET
SDK `11.0.100-rc.1.26425.128` and headless Chromium.

CoreCLR and NativeAOT use 21 samples with rotating variant order. Browser
measurements use 21 samples and calibrate batches toward 20 ms, capped at 1,000
operations. Calibration uses the shortest of five warmed probes so a GC pause
cannot select an undersized batch for the browser's 100-microsecond clock.

The Browser host links the exact kernel used by the console probe. It verifies
before measurement that:

- opening the managed session does not start scanning;
- constructing the `ReadableStream` does not start scanning;
- the first reader pull starts production;
- Complete, Oracle, Pull, and WebStream produce the same byte count and 64-bit
  checksum; and
- WebStream preserves Pull's scan, emitted-row, and pull counts.

Raw generated CSV remains under ignored `artifacts/sourcepull/`.

## NativeAOT kernel

Times are medians in microseconds. Ratios are relative to the specialized
oracle.

<!-- markdownlint-disable MD013 -->

| Corpus and plan | Complete | Oracle | Pull | Complete/oracle | Pull/oracle | Complete allocation | Pull allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10,000-line Rows | 2,490.653 | 1,825.563 | 1,838.778 | 1.364 | 1.007 | 6,010,408 B | 4,872 B |
| Clarissa Rows | 2,158.534 | 1,663.621 | 1,700.750 | 1.297 | 1.022 | 6,135,360 B | 9,248 B |
| 10,000-line Head(10) | 138.438 | 1.963 | 2.144 | 70.524 | 1.092 | 1,817,792 B | 4,872 B |
| Clarissa Head(10) | 177.351 | 1.723 | 1.803 | 102.932 | 1.046 | 1,942,840 B | 9,248 B |
| 10,000-line filtered Head(10) | 134.951 | 2.725 | 2.865 | 49.523 | 1.051 | 1,819,864 B | 4,872 B |
| Clarissa filtered Head(10) | 196.407 | 2.225 | 2.375 | 88.273 | 1.067 | 1,942,840 B | 9,248 B |

<!-- markdownlint-enable MD013 -->

Complete Rows costs 30-36% more than the oracle and retains about 6 MB.
Pull Rows is within 1-2% of the oracle and retains one reader buffer plus one
encoded frame. On bounded large-document plans, Pull is within 5-9% of the
oracle while Complete performs 50-103 times the oracle work.

Small inputs retain a fixed pull cost of roughly 1-3 microseconds and about
4.2 KiB beyond the oracle, almost entirely the reader buffer. A production path
therefore needs a complete-path fast lane.

## Browser/Wasm AOT crossover

Times are medians in microseconds. `Complete` is the fully materialized managed
path. `WebStream` includes managed session creation, JS interop, a cold WHATWG
stream, 4-KiB chunk copies, and reader consumption.

<!-- markdownlint-disable MD013 -->

| UTF-8 input | Rows Complete/WebStream | Count Complete/WebStream | Head(10) Complete/WebStream | Filtered Head Complete/WebStream |
| ---: | ---: | ---: | ---: | ---: |
| 5,050 B | 49.0 / 55.2 | 6.0 / 9.4 | 9.0 / 13.3 | 11.0 / 15.4 |
| 12,784 B | 120.5 / 126.9 | 13.8 / 10.9 | 14.5 / 12.9 | 17.0 / 15.4 |
| 25,678 B | 238.0 / 242.6 | 27.0 / 12.9 | 23.5 / 13.0 | 26.0 / 14.9 |
| 51,462 B | 486.0 / 482.4 | 73.0 / 17.4 | 62.5 / 13.0 | 64.5 / 15.0 |
| 130,322 B | 1,221.1 / 1,178.9 | 195.0 / 31.8 | 166.5 / 13.0 | 168.0 / 15.4 |
| 524,606 B | 5,100.0 / 4,780.0 | 815.0 / 100.0 | 670.1 / 13.4 | 676.0 / 15.3 |

<!-- markdownlint-enable MD013 -->

The observed AOT crossover is between 5 and 13 KiB for Count and bounded Head,
and between 26 and 51 KiB for complete Rows. These are prototype observations,
not production constants: the corpus is synthetic, acquisition and decoding
are excluded, and real source distributions still need measurement.

After crossover, the benefit grows with work avoided. At 524 KiB, WebStream is
about 8 times faster than Complete for Count, 50 times faster for Head(10), and
44 times faster for filtered Head(10). Full Rows is only about 6% faster because
both paths must encode and consume every row.

The AOT WebStream adds about 9 microseconds over managed Pull for bounded
results. That fixed cost explains why small documents favor Complete even
though the managed pull kernel itself remains close to the oracle.

Published interpreter results show the same shape with more tiering and layout
noise. At 524 KiB, interpreter WebStream Rows takes 19.9 ms versus 4.8 ms under
AOT, and bounded Head takes 30.8 microseconds versus 13.4 microseconds.

## Browser payload

Sizes cover one clean published `wwwroot`. Brotli size is the sum of generated
`.br` assets and represents the deployable transfer-oriented comparison.

| Mode | Uncompressed | Brotli | Native Wasm |
| --- | ---: | ---: | ---: |
| Interpreter | 7,714,256 B | 2,150,775 B | 3,010,488 B |
| Mono AOT | 12,603,491 B | 3,114,515 B | 7,898,002 B |
| AOT increase | 4,889,235 B | 963,740 B | 4,887,514 B |

Mono AOT makes the measured end-to-end path roughly 2-4 times faster, depending
on the plan and amount of output, at a cost of about 0.97 MB additional Brotli
payload for this standalone app.

## Production implications

A focused House Source design should carry:

1. an exact settled decoded-size or UTF-8-size hint from the serving boundary;
2. a closed terminal classification, since the crossover differs materially
   between Rows, Count, and bounded Head;
3. a small-input complete fast path and a large/selective cold pull path;
4. bounded buffer ownership and cancellation from the browser reader back to
   scanning; and
5. Before/Naive/Oracle/After evidence through real acquisition, decoding,
   projection, serialization, transport, and UI consumption.

The prototype begins with an already decoded immutable string. It does not
settle incremental HTTP acquisition, compression, decoding, source checksum,
or oversized failures in an unrequested tail. Those remain production-design
questions rather than evidence supplied by this experiment.
