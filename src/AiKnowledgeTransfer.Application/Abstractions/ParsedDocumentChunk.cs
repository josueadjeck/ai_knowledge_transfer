namespace AiKnowledgeTransfer.Application.Abstractions;

public sealed record ParsedDocumentChunk(
    int ChunkNumber,
    string Text,
    int StartCharacter,
    int EndCharacter,
    string SourceReference = "Document text",
    string QualityStatus = "Unassessed");
