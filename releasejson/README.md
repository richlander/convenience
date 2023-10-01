# Release.json Test app

The [releasejson](releasejson) app explores the convenience spectrum of [`System.Text.Json`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/how-to) APIs. It does that via processing [JSON documents](fakejson) of varying lengths. They are fake test versions of the official [release.json](https://github.com/dotnet/core/blob/main/release-notes/releases-index.json) documents maintained by the .NET Team at Microsoft.

The app accesses the JSON documents in three different ways: remotely via Github `raw.` HTTPS URLs, locally via a static files [web app](fakejsonweb), and locally via the file system.

The results are discussed on the [.NET blog](https://devblogs.microsoft.com/dotnet/).
