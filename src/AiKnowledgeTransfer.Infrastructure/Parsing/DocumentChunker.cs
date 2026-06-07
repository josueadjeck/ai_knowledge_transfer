namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using AiKnowledgeTransfer.Application.Abstractions;

internal static class DocumentChunker
{
    private const int MaxChunkLength = 1200;

    public static IReadOnlyCollection<ParsedDocumentChunk> SplitIntoChunks(string text)
    {
        var chunks = new List<ParsedDocumentChunk>();
        var chunkNumber = 1;

        foreach (var paragraph in ReadParagraphs(text))
        {
            var remaining = paragraph.Text.Trim();
            var start = paragraph.StartCharacter;

            while (remaining.Length > 0)
            {
                var length = Math.Min(MaxChunkLength, remaining.Length);
                var chunkText = remaining[..length].Trim();

                if (chunkText.Length > 0)
                {
                    chunks.Add(new ParsedDocumentChunk(
                        chunkNumber++,
                        chunkText,
                        start,
                        start + chunkText.Length));
                }

                remaining = remaining[length..].TrimStart();
                start += length;
            }
        }

        return chunks;
    }

    private static IEnumerable<(string Text, int StartCharacter)> ReadParagraphs(string text)
    {
        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = 0;

        foreach (var paragraph in normalizedText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = normalizedText.IndexOf(paragraph, start, StringComparison.Ordinal);
            if (index < 0)
            {
                index = start;
            }

            yield return (paragraph, index);
            start = index + paragraph.Length;
        }
    }
}
