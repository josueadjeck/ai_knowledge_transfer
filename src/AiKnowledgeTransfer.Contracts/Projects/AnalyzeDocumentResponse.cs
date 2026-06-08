namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record AnalyzeDocumentResponse(
    Guid DocumentId,
    string Status,
    int ChunkCount,
    string ParserName,
    string Detail,
    IReadOnlyCollection<DocumentChunkResponse> Chunks);
