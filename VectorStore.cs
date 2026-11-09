using System.Text.Json;

namespace RagExample;

public record Chunk(string Source, int Index, string Text, float[] Vector);

public record SearchHit(Chunk Chunk, float Score);

public class VectorStore
{
    public List<Chunk> Chunks { get; init; } = [];

    public void Add(Chunk chunk) => Chunks.Add(chunk);

    public List<SearchHit> Search(float[] query, int topK) =>
        Chunks.Select(c => new SearchHit(c, CosineSimilarity(query, c.Vector)))
              .OrderByDescending(h => h.Score)
              .Take(topK)
              .ToList();

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Різна розмірність векторів: {a.Length} vs {b.Length}. Індекс будувався іншою моделлю? Запусти з --reindex.");

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return normA == 0 || normB == 0 ? 0 : (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
    }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(Chunks));

    public static VectorStore Load(string path) =>
        new() { Chunks = JsonSerializer.Deserialize<List<Chunk>>(File.ReadAllText(path)) ?? [] };
}
