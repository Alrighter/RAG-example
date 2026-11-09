using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RagExample;

public class OpenAiEmbedder(HttpClient http, string baseUrl, string model, string apiKey)
{
    private record EmbedRequest(string Model, string[] Input);
    private record EmbeddingItem(int Index, float[] Embedding);
    private record EmbedResponse(EmbeddingItem[] Data);

    public async Task<float[][]> EmbedAsync(IReadOnlyList<string> texts)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/embeddings")
        {
            Content = JsonContent.Create(new EmbedRequest(model, texts.ToArray()))
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Embeddings {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");

        var body = await resp.Content.ReadFromJsonAsync<EmbedResponse>()
                   ?? throw new InvalidOperationException("Порожня відповідь від /embeddings.");

        // порядок у відповіді гарантує поле index, а не позиція в масиві
        return body.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToArray();
    }
}
