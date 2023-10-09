# Word count

This code explores the convenience spectrum of File I/O APIs. The app is intended to mimic the behavior of the [wc](https://github.com/coreutils/coreutils/blob/master/src/wc.c) tool, which also means that it can be used as a baseline.

The same word counting behavior is written multiple ways, using the following APIs, most to least convenience (AKA high to low level).

* `File.ReadLines+IEnumerable<string>`
* `File.OpenText+StreamReader.ReadLine`
* `File.Open+FileStream.Read`
* `File.OpenHandle+RandomAccess.Read`

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
11716 110023 598796
11716 110023 598796
```

The character counts don't match `wc` for the implementations using the high-level APIs. This is due to a combination of control characters, including newlines that are elided by these APIs. There wasn't any obvious way to rememdy that (beyond adding the line count to the character count). `wc` is written using low-level approaches, so is most directly comparable to the most low-level .NET APIs.

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

Note: `File_OpenHandle_RandomAccess` was used as the implementation for the `wordcount` tool

## Performance with Benchmarkdotnet

```
BenchmarkDotNet=v0.13.5, OS=ubuntu 22.04
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.105
  [Host]     : .NET 7.0.5 (7.0.523.17801), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.5 (7.0.523.17801), X64 RyuJIT AVX2


|                               Method |       Mean |    Error |   StdDev | Ratio |     Gen0 |     Gen1 |    Gen2 | Allocated | Alloc Ratio |
|------------------------------------- |-----------:|---------:|---------:|------:|---------:|---------:|--------:|----------:|------------:|
|                    File_ReadAllLines | 3,657.5 us |  9.05 us |  7.55 us |  2.05 | 277.3438 | 195.3125 | 93.7500 | 1948922 B |    3,677.21 |
|                       File_ReadLines | 2,825.8 us |  6.39 us |  5.98 us |  1.58 | 378.9063 |        - |       - | 1592707 B |    3,005.11 |
|                        File_OpenText | 2,923.6 us | 15.67 us | 14.66 us |  1.64 | 378.9063 |        - |       - | 1592651 B |    3,005.00 |
|                            File_Open | 1,973.6 us |  1.92 us |  1.79 us |  1.11 |        - |        - |       - |     684 B |        1.29 |
|         File_OpenHandle_RandomAccess | 1,784.0 us |  5.40 us |  4.51 us |  1.00 |        - |        - |       - |     530 B |        1.00 |
| File_OpenHandle_RandomAccess_IndexOf |   844.2 us |  2.78 us |  2.32 us |  0.47 |        - |        - |       - |     529 B |        1.00 |
```
