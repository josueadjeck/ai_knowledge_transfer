namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record KnowledgeReviewHistoryResponse(
    Guid Id,
    string Action,
    string ReviewStatus,
    string QualityStatus,
    string Reviewer,
    string Comment,
    DateTimeOffset CreatedAt);
