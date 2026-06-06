namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;

public sealed class PlainTextDocumentParser : IDocumentParser
{
    private const int MaxChunkLength = 1200;

    public bool CanParse(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedDocument> ParseAsync(
        string contentType,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        var chunks = SplitIntoChunks(text);

        return new ParsedDocument(chunks);
    }

    private static IReadOnlyCollection<ParsedDocumentChunk> SplitIntoChunks(string text)
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
