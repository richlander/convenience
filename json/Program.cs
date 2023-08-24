using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Running;
using ReleaseJson;
using Tests;

// BenchmarkRunner.Run(typeof(JsonTests));

// Console.WriteLine("Press any key");
// Console.ReadKey();

var json = await Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run();
Console.WriteLine(json);

//await NewtonsoftJsonSerializerBenchmark.NewtonsoftJsonSerializerBenchmark.Run();

// List<BenchmarkResult> results =
// [
//     new (nameof(JsonSerializerBenchmark.JsonSerializerBenchmark), JsonSerializerBenchmark.JsonSerializerBenchmark.Run()),
//     new (nameof(JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark), JsonSerializerSourceGeneratorBenchmark.JsonSerializerSourceGeneratorBenchmark.Run()),
//     new (nameof(JsonDocumentBenchmark.JsonDocumentBenchmark), JsonDocumentBenchmark.JsonDocumentBenchmark.Run()),
//     new (nameof(Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark), Utf8JsonReaderWriterBenchmark.Utf8JsonReaderWriterBenchmark.Run())
// ];

// foreach (var result in results)
// {
//     var json = await result.Result;
//     Console.WriteLine($"*********{result.Name}");
//     Console.WriteLine(json);
//     Console.WriteLine($"Length: {json.Length}");
// }

// public record BenchmarkResult(string Name, Task<string> Result);
