namespace AiKnowledgeTransfer.Application.Abstractions;

public sealed record ParsedDocument(
    IReadOnlyCollection<ParsedDocumentChunk> Chunks,
    string ParserName = "Unknown",
    string Detail = "");
