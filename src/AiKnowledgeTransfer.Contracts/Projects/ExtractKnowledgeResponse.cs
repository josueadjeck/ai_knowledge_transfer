namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record ExtractKnowledgeResponse(
    Guid DocumentId,
    int CreatedItemCount,
    string ProviderName,
    bool UsedFallback,
    string Detail,
    IReadOnlyCollection<KnowledgeItemResponse> KnowledgeItems);
