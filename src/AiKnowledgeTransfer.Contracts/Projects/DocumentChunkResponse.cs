namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentChunkResponse(
    Guid Id,
    Guid DocumentId,
    int ChunkNumber,
    string Text,
    int StartCharacter,
    int EndCharacter,
    string SourceReference,
    DateTimeOffset CreatedAt);
