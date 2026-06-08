namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

public sealed class WordDocumentParser : IDocumentParser
{
    private const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public string Name => nameof(WordDocumentParser);

    public IReadOnlyCollection<string> SupportedContentTypes =>
    [
        WordContentType
    ];

    public IReadOnlyCollection<string> SupportedFileExtensions =>
    [
        ".docx"
    ];

    public string CapabilityDescription => "Word .docx paragraphs are extracted from the document body.";

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
        var chunks = DocumentChunker.SplitIntoChunks(text, "Word document body");
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Word document did not contain extractable text.");
        }

        return Task.FromResult(new ParsedDocument(
            chunks,
            Name,
            $"Word parser created {chunks.Count} chunks."));
    }

    private static string ExtractText(Stream content, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var document = WordprocessingDocument.Open(content, isEditable: false);
        var mainDocument = document.MainDocumentPart?.Document;
        var body = mainDocument?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        foreach (var paragraph in body.Elements<Paragraph>())
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
}
