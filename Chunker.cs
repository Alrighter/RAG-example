namespace RagExample;

public static class Chunker
{
    private static readonly string[] Separators = ["\n\n", ". ", "\n", " "];

    public static List<string> Split(string text, int chunkSize, int overlap)
    {
        text = text.Replace("\r\n", "\n").Trim();
        var chunks = new List<string>();
        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);
            if (end < text.Length)
                end = FindCut(text, start, end);

            var chunk = text[start..end].Trim();
            if (chunk.Length > 0) chunks.Add(chunk);
            if (end >= text.Length) break;

            int next = Math.Max(end - overlap, start + 1);
            int space = text.IndexOfAny([' ', '\n'], next, end - next);
            start = space >= 0 ? space + 1 : next;
        }
        return chunks;
    }

    private static int FindCut(string text, int start, int end)
    {
        int minCut = start + (end - start) / 2;

        foreach (var sep in Separators)
        {
            int idx = text.LastIndexOf(sep, end - 1, end - minCut, StringComparison.Ordinal);
            if (idx >= minCut) return idx + sep.Length;
        }
        return end;
    }
}
