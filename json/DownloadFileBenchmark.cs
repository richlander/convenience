namespace DownloadFileBenchmark;
public static class DownloadFileBenchmark
{
    public static async Task<string> Run()
    {
        var httpClient = new HttpClient();
        return await httpClient.GetStringAsync(FakeTestData.URL);
    }
}