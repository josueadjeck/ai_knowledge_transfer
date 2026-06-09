namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record RoadmapPhaseReadinessResponse(
    int Phase,
    string Name,
    string Status,
    int CompletedCriteria,
    int TotalCriteria,
    IReadOnlyCollection<string> Evidence,
    IReadOnlyCollection<string> Gaps);
