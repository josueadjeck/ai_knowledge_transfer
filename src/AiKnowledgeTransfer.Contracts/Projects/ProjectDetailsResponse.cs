namespace AiKnowledgeTransfer.Contracts.Projects;

using AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record ProjectDetailsResponse(
    Guid Id,
    string Name,
    string Description,
    string Owner,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<DocumentResponse> Documents,
    IReadOnlyCollection<KnowledgeItemResponse> KnowledgeItems,
    IReadOnlyCollection<RoadmapResponse> Roadmaps);
