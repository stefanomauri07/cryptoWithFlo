using System.Text;
using System.Text.Json;

namespace CryptoApp.Services;

public class OllamaService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["Ollama:Model"] ?? "nemotron-3-super:cloud";
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            stream = false,
            options = new { temperature = 0.7, num_predict = 1024 }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/chat", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
        return result?.Message?.Content ?? "No response from AI";
    }
}

public class OllamaChatResponse
{
    public OllamaMessage? Message { get; set; }
}

public class OllamaMessage
{
    public string? Content { get; set; }
}
