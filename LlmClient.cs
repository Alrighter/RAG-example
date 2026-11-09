using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RagExample;

public class LlmClient(HttpClient http, string baseUrl, string model, string apiKey)
{
    private record Message(string Role, string? Content);
    private record ChatRequest(string Model, Message[] Messages, double Temperature,
        [property: JsonPropertyName("chat_template_kwargs")] Dictionary<string, object> ChatTemplateKwargs);
    private record Choice(Message Message);
    private record ChatResponse(Choice[] Choices);

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt)
    {
        var payload = new ChatRequest(
            model,
            [new Message("system", systemPrompt), new Message("user", userPrompt)],
            Temperature: 0.2,
            ChatTemplateKwargs: new() { ["enable_thinking"] = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"LLM {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");

        var body = await resp.Content.ReadFromJsonAsync<ChatResponse>();
        var text = body?.Choices.FirstOrDefault()?.Message.Content ?? "";

        return Regex.Replace(text, "<think>.*?</think>", "", RegexOptions.Singleline).Trim();
    }
}
