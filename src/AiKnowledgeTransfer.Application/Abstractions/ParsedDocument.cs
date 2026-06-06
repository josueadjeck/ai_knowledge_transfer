namespace AiKnowledgeTransfer.Application.Abstractions;

public sealed record ParsedDocument(
    IReadOnlyCollection<ParsedDocumentChunk> Chunks);
