namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record OnboardingReadinessResponse(
    Guid ProjectId,
    string Status,
    bool CanStartOnboarding,
    int FinalKnowledgeCount,
    int ReviewRiskCount,
    int RoadmapCount,
    string? LatestRoadmapTargetRole,
    IReadOnlyCollection<string> Recommendations);
