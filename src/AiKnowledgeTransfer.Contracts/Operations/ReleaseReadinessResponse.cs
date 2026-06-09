namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record ReleaseReadinessResponse(
    string Status,
    string Service,
    DateTimeOffset CheckedAt,
    IReadOnlyCollection<ReleaseReadinessCheckResponse> Checks);
