using System.Text;

namespace RagExample;

public static class PromptBuilder
{
    private const string SystemPrompt =
        """
        Ти асистент, який відповідає ТІЛЬКИ на основі наданого контексту.
        Якщо відповіді в контексті немає — чесно скажи: "У документах немає цієї інформації."
        Не вигадуй фактів. Після кожного твердження вказуй номер джерела у форматі [1], [2].
        Відповідай українською, максимально коротко і по суті.
        """;

    public static (string System, string User) Build(string question, IReadOnlyList<SearchHit> hits)
    {
        var sb = new StringBuilder("Контекст:\n\n");
        for (int i = 0; i < hits.Count; i++)
        {
            var c = hits[i].Chunk;
            sb.AppendLine($"[{i + 1}] (джерело: {c.Source}, фрагмент #{c.Index})");
            sb.AppendLine(c.Text);
            sb.AppendLine();
        }
        sb.AppendLine($"Питання: {question}");
        return (SystemPrompt, sb.ToString());
    }
}
