# Word count

This code explores the convenience spectrum of File I/O APIs. The app is intended to mimic the behavior of the [wc](https://github.com/coreutils/coreutils/blob/master/src/wc.c) tool, which also means that it can be used as a baseline.

The same word counting behavior is written multiple ways, using the following APIs, most to least convenience (AKA high to low level).

* `File.ReadAllLines+string[]`
* `File.ReadLines+IEnumerable<string>`
* `File.OpenText+StreamReader.ReadLine`
* `File.Open+FileStream.Read`
* `File.OpenHandle+RandomAccess.Read`

There are two different approaches using `RandomAccess.Read`.

The included [`text.txt`](text.txt) file is a copy of [Clarissa Harlowe; or the history of a young lady — Volume 1 by Samuel Richardson](https://www.gutenberg.org/ebooks/9296). This text was chosen because it was on a list of long books and was freely available on [Project Gutenberg](https://www.gutenberg.org/).

## Behavior

Output of code with `wc` baseline.

The form is ascending order of count:

Lines Words Characters

```bash
$ wc text.txt 
 11716 110023 598796 text.txt
$ dotnet run -c Release
11716 110023 587080
11716 110023 587080
11716 110023 587080
11716 110023 598796
11716 110023 598796
11716 110023 598796
```

The character counts don't match for the implementations using the high-level APIs. This is due to a combination of control characters, including newlines that are elided by these APIs. There wasn't any obvious way to rememdy that (beyond adding the line count to the character count). `wc` is written using low-level approaches, so is most directly comparable to the most low-level .NET APIs.

## Performance with `time`

```
$ time wc text.txt
 11716 110023 598796 text.txt

real	0m0.008s
user	0m0.007s
sys	0m0.001s
$ time ./bin/Release/net7.0/wordcount 
11716 110023 598796

real	0m0.048s
user	0m0.036s
sys	0m0.012s
```

Note: `File_OpenHandle_RandomAccess_IndexOf` was used as the implementation for the `wordcount` tool

## Performance with Benchmarkdotnet

```
|                               Method |     Mean |     Error |    StdDev | Ratio | RatioSD |     Gen0 |     Gen1 |    Gen2 | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|--------:|----------:|------------:|
|                    File_ReadAllLines | 3.680 ms | 0.0185 ms | 0.0164 ms |  2.04 |    0.02 | 277.3438 | 195.3125 | 93.7500 | 1948923 B |    3,677.21 |
|                       File_ReadLines | 2.849 ms | 0.0335 ms | 0.0313 ms |  1.58 |    0.02 | 378.9063 |        - |       - | 1592707 B |    3,005.11 |
|                        File_OpenText | 2.940 ms | 0.0304 ms | 0.0284 ms |  1.63 |    0.02 | 378.9063 |        - |       - | 1592651 B |    3,005.00 |
|                            File_Open | 1.988 ms | 0.0190 ms | 0.0178 ms |  1.10 |    0.01 |        - |        - |       - |     684 B |        1.29 |
|         File_OpenHandle_RandomAccess | 1.803 ms | 0.0116 ms | 0.0109 ms |  1.00 |    0.00 |        - |        - |       - |     530 B |        1.00 |
| File_OpenHandle_RandomAccess_IndexOf | 1.240 ms | 0.0239 ms | 0.0265 ms |  0.69 |    0.02 |        - |        - |       - |     530 B |        1.00 |
```
