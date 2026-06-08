namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig;

public sealed class PdfDocumentParser : IDocumentParser
{
    public string Name => nameof(PdfDocumentParser);

    public IReadOnlyCollection<string> SupportedContentTypes =>
    [
        "application/pdf"
    ];

    public IReadOnlyCollection<string> SupportedFileExtensions =>
    [
        ".pdf"
    ];

    public string CapabilityDescription => "Text-based PDFs are parsed with PdfPig; scanned PDFs require OCR before analysis.";

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
            throw new InvalidOperationException("PDF did not contain extractable text. Scanned PDFs need OCR before analysis.");
        }

        return Task.FromResult(new ParsedDocument(
            chunks,
            Name,
            $"PDF text parser created {chunks.Count} chunks. Scanned PDFs still require OCR."));
    }

    private static string ExtractText(Stream content, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var pdf = OpenPdf(content);

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

    private static PdfDocument OpenPdf(Stream content)
    {
        try
        {
            return PdfDocument.Open(content);
        }
        catch (PdfDocumentFormatException exception)
        {
            throw new InvalidOperationException("PDF could not be parsed. Ensure the file is valid; scanned PDFs need OCR before analysis.", exception);
        }
    }
}
