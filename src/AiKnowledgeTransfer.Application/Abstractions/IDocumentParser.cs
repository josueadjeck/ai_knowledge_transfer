namespace AiKnowledgeTransfer.Application.Abstractions;

public interface IDocumentParser
{
    string Name { get; }

    IReadOnlyCollection<string> SupportedContentTypes { get; }

    IReadOnlyCollection<string> SupportedFileExtensions { get; }

    string CapabilityDescription { get; }

    bool CanParse(string contentType, string fileName);

    Task<ParsedDocument> ParseAsync(
        string contentType,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);
}
