namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record ReleaseReadinessCheckResponse(
    string Name,
    string Status,
    bool Required,
    string Detail,
    string Evidence);
