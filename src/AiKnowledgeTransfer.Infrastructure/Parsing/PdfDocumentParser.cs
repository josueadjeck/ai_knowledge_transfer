namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;
using UglyToad.PdfPig;

public sealed class PdfDocumentParser : IDocumentParser
{
    public bool CanParse(string contentType, string fileName)
    {
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ParsedDocument> ParseAsync(
        string contentType,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var text = ExtractText(content, cancellationToken);
        var chunks = DocumentChunker.SplitIntoChunks(text);
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("PDF did not contain extractable text.");
        }

        return Task.FromResult(new ParsedDocument(chunks));
    }

    private static string ExtractText(Stream content, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var pdf = PdfDocument.Open(content);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(page.Text))
            {
                builder.AppendLine(page.Text.Trim());
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
