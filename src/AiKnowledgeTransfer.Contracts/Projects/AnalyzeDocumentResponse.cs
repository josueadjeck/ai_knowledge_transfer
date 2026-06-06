namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record AnalyzeDocumentResponse(
    Guid DocumentId,
    string Status,
    int ChunkCount,
    IReadOnlyCollection<DocumentChunkResponse> Chunks);
