namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string Description,
    string Owner);
