namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentChunkResponse(
    Guid Id,
    int ChunkNumber,
    string Text,
    int StartCharacter,
    int EndCharacter,
    DateTimeOffset CreatedAt);
