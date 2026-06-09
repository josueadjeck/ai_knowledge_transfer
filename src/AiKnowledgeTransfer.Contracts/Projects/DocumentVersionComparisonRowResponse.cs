namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentVersionComparisonRowResponse(
    int RowNumber,
    string ChangeType,
    int? BaseChunkNumber,
    int? TargetChunkNumber,
    string BaseText,
    string TargetText,
    string BaseQualityStatus,
    string TargetQualityStatus);
