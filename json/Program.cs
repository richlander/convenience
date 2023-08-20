using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Running;
using ReleaseJson;
using Tests;

BenchmarkRunner.Run(typeof(JsonTests));

// List<BenchmarkResult> results =
// [
//     new (nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), JsonSerializerBenchmark.JsonSerializerBenchmark.Run()),
//     new (nameof(JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark), JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run()),
//     new (nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), JsonDocumentBenchmark.JsonDocumentBenchmark.Run()),
//     new (nameof(Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark), Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run())
// ];

// foreach (var result in results)
// {
//     Console.WriteLine($"*********{result.Name}");
//     Console.WriteLine(await result.Result);
// }

// public record BenchmarkResult(string Name, Task<string> Result);
