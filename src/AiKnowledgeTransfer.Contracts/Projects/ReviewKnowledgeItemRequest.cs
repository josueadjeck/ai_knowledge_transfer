namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record ReviewKnowledgeItemRequest(
    string Reviewer,
    string Comment);
