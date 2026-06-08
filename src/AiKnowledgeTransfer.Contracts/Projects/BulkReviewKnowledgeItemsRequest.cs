namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record BulkReviewKnowledgeItemsRequest(
    IReadOnlyCollection<Guid> KnowledgeItemIds,
    string Reviewer,
    string Comment,
    string? QualityStatus = null);
