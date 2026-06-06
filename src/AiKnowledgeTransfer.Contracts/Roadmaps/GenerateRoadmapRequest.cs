namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record GenerateRoadmapRequest(
    string TargetRole,
    int DurationInWeeks);
