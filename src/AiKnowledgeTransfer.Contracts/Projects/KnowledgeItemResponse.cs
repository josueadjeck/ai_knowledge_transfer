namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record KnowledgeItemResponse(
    Guid Id,
    string Type,
    string Title,
    string Summary,
    Guid? SourceDocumentId,
    int? SourceChunkNumber,
    string ExtractionProvider,
    string? ExtractionModel,
    bool ExtractionUsedFallback,
    string ExtractionQuality,
    string ReviewStatus,
    string? ReviewedBy,
    string? ReviewComment,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt);
