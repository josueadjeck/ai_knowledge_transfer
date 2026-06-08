namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record OnboardingStartPackageResponse(
    Guid ProjectId,
    string ProjectName,
    string Status,
    bool CanStartOnboarding,
    string? TargetRole,
    IReadOnlyCollection<RoadmapWeekResponse> FirstWeeks,
    IReadOnlyCollection<string> StarterTasks,
    IReadOnlyCollection<string> ReviewWarnings);
