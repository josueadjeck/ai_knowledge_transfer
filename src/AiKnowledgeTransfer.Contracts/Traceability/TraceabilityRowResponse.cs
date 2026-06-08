namespace AiKnowledgeTransfer.Contracts.Traceability;

public sealed record TraceabilityRowResponse(
    Guid DocumentId,
    string DocumentName,
    string DocumentStatus,
    int ChunkCount,
    Guid? KnowledgeItemId,
    string? KnowledgeType,
    string? KnowledgeTitle,
    int? SourceChunkNumber,
    string? ExtractionProvider,
    string? ExtractionModel,
    bool ExtractionUsedFallback,
    string? ExtractionQuality,
    string? KnowledgeReviewStatus,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    int RoadmapUsageCount,
    bool IncludedInExport);
