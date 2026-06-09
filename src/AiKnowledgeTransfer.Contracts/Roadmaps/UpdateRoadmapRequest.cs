namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record UpdateRoadmapRequest(
    IReadOnlyCollection<UpdateRoadmapWeekRequest> Weeks);
