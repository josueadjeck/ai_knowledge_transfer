namespace AiKnowledgeTransfer.Infrastructure.Parsing;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;

public sealed class PlainTextDocumentParser : IDocumentParser
{
    public string Name => nameof(PlainTextDocumentParser);

    public IReadOnlyCollection<string> SupportedContentTypes =>
    [
        "text/*",
        "application/markdown"
    ];

    public IReadOnlyCollection<string> SupportedFileExtensions =>
    [
        ".txt",
        ".md",
        ".markdown"
    ];

    public string CapabilityDescription => "Text and Markdown documents are parsed as UTF-8 text.";

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
        var chunks = DocumentChunker.SplitIntoChunks(text, "Plain text body");

        return new ParsedDocument(
            chunks,
            Name,
            $"Text parser created {chunks.Count} chunks.");
    }
}
