namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record KnowledgeReviewSummaryResponse(
    Guid ProjectId,
    int TotalCount,
    int FinalCount,
    IReadOnlyCollection<KnowledgeReviewSummaryGroupResponse> ByReviewStatus,
    IReadOnlyCollection<KnowledgeReviewSummaryGroupResponse> ByQualityStatus,
    IReadOnlyCollection<KnowledgeReviewSummaryGroupResponse> ByType);

public sealed record KnowledgeReviewSummaryGroupResponse(string Name, int Count);
