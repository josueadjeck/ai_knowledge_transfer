namespace AiKnowledgeTransfer.Application.Abstractions;

public interface IDocumentParser
{
    bool CanParse(string contentType, string fileName);

    Task<ParsedDocument> ParseAsync(
        string contentType,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);
}
