namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentChunkQualitySummaryResponse(
    string QualityStatus,
    int Count);
