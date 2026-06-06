namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record ExtractKnowledgeResponse(
    Guid DocumentId,
    int CreatedItemCount,
    IReadOnlyCollection<KnowledgeItemResponse> KnowledgeItems);
