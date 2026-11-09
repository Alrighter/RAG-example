using System.Text;
using RagExample;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

string llmBaseUrl     = Env("LLM_BASE_URL", "http://localhost:8000/v1");
string llmModel       = Env("LLM_MODEL", "qwen");
string llmApiKey      = Env("LLM_API_KEY", "dummy");
string embeddingModel = Env("EMBEDDING_MODEL", "unsloth/bge-small-en-v1.5");
string docsFolder     = Env("DOCS_FOLDER", "docs");

const string IndexFile = "index.json";
const int ChunkSize = 800;
const int ChunkOverlap = 150;
const int TopK = 4;

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
var embedder = new OpenAiEmbedder(http, llmBaseUrl, embeddingModel, llmApiKey);
var llm = new LlmClient(http, llmBaseUrl, llmModel, llmApiKey);

VectorStore store;
if (!args.Contains("--reindex") && File.Exists(IndexFile))
{
    store = VectorStore.Load(IndexFile);
    Console.WriteLine($"Індекс завантажено з {IndexFile}: {store.Chunks.Count} чанків. (--reindex щоб перебудувати)");
}
else
{
    store = await BuildIndexAsync();
    store.Save(IndexFile);
    Console.WriteLine($"Індекс збережено в {IndexFile}: {store.Chunks.Count} чанків.");
}

Console.WriteLine("\nСтав питання. '!' на початку — відповідь БЕЗ RAG (для порівняння). 'exit' — вихід.");
while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine();
    if (input is null || input.Trim() == "exit") break;
    input = input.Trim();
    if (input.Length == 0) continue;

    try
    {
        if (input.StartsWith('!'))
        {
            var plain = await llm.ChatAsync("Ти корисний асистент. Відповідай українською.", input[1..].Trim());
            Console.WriteLine($"\n[БЕЗ RAG]\n{plain}");
            continue;
        }

        var queryVector = (await embedder.EmbedAsync([input]))[0];
        var hits = store.Search(queryVector, TopK);

        Console.WriteLine("\nЗнайдені фрагменти:");
        for (int i = 0; i < hits.Count; i++)
            Console.WriteLine($"  [{i + 1}] {hits[i].Chunk.Source} #{hits[i].Chunk.Index}  score={hits[i].Score:F3}");

        var (system, user) = PromptBuilder.Build(input, hits);
        var answer = await llm.ChatAsync(system, user);
        Console.WriteLine($"\n[RAG]\n{answer}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка: {ex.Message}");
    }
}

async Task<VectorStore> BuildIndexAsync()
{
    if (!Directory.Exists(docsFolder))
        throw new DirectoryNotFoundException($"Немає папки з документами: {Path.GetFullPath(docsFolder)}");

    var files = Directory.EnumerateFiles(docsFolder, "*.*", SearchOption.AllDirectories)
        .Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        .ToList();
    if (files.Count == 0)
        throw new InvalidOperationException($"У {docsFolder} немає .md/.txt файлів.");

    var result = new VectorStore();
    Console.WriteLine("Індексація документів...");
    foreach (var file in files)
    {
        var parts = Chunker.Split(File.ReadAllText(file), ChunkSize, ChunkOverlap);

        for (int i = 0; i < parts.Count; i += 16)
        {
            var batch = parts.Skip(i).Take(16).ToList();
            var vectors = await embedder.EmbedAsync(batch);
            for (int j = 0; j < batch.Count; j++)
                result.Add(new Chunk(Path.GetFileName(file), i + j, batch[j], vectors[j]));
        }
        Console.WriteLine($"  {Path.GetFileName(file)}: {parts.Count} чанків");
    }
    return result;
}

static string Env(string name, string fallback) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;
