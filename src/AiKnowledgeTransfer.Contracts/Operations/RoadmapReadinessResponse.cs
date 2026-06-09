namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record RoadmapReadinessResponse(
    string Status,
    DateTimeOffset GeneratedAt,
    int PhaseCount,
    int ReadyPhaseCount,
    int NeedsWorkPhaseCount,
    IReadOnlyCollection<RoadmapPhaseReadinessResponse> Phases,
    IReadOnlyCollection<string> Summary);
