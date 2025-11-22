using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
 namespace ElectionShield.Services { 
public class AiService
{
    private readonly HttpClient _httpClient;

    public AiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> AnalyzeFileAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        using var stream = File.OpenRead(filePath);

        form.Add(new StreamContent(stream), "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync("http://127.0.0.1:8000/analyze", form);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
}
