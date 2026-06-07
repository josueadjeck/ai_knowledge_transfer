namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

public sealed class WordDocumentParser : IDocumentParser
{
    private const int MaxChunkLength = 1200;
    private const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool CanParse(string contentType, string fileName)
    {
        return contentType.Equals(WordContentType, StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(fileName).Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ParsedDocument> ParseAsync(
        string contentType,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var text = ExtractText(content, cancellationToken);
        var chunks = SplitIntoChunks(text);
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Word document did not contain extractable text.");
        }

        return Task.FromResult(new ParsedDocument(chunks));
    }

    private static string ExtractText(Stream content, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var document = WordprocessingDocument.Open(content, isEditable: false);
        var paragraphs = document.MainDocumentPart?.Document.Body?.Elements<Paragraph>() ?? [];

        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = paragraph.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                builder.AppendLine(text);
                builder.AppendLine();
            }
        }

        return builder.ToString();
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
