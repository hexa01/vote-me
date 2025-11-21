using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly string _aiUrl = "http://127.0.0.1:8000/analyze"; //port change

    public AiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> AnalyzeFileAsync(string filePath)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync(_aiUrl, content);
        return await response.Content.ReadAsStringAsync();
    }
}
