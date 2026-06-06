namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record RoadmapResponse(
    Guid Id,
    string TargetRole,
    int DurationInWeeks,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<RoadmapWeekResponse> Weeks);
