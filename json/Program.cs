using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Running;
using ReleaseJson;
using Tests;

// BenchmarkRunner.Run(typeof(JsonTests));

await JsonWithUtf8.Go();
// Console.WriteLine(Encoding.UTF8.GetChars(json));





// (int)((DateTime.Now.Ticks - day.Ticks) / TimeSpan.TicksPerDay)

// DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulure)