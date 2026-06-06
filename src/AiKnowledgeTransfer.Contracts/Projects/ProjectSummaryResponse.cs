namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record ProjectSummaryResponse(
    Guid Id,
    string Name,
    string Description,
    string Owner,
    DateTimeOffset CreatedAt,
    int DocumentCount,
    int KnowledgeItemCount,
    int RoadmapCount);
