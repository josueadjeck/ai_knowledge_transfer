namespace AiKnowledgeTransfer.Contracts.Diagnostics;

public sealed record HealthResponse(
    string Status,
    string Service,
    DateTimeOffset Timestamp,
    IReadOnlyCollection<HealthComponentResponse> Components);
