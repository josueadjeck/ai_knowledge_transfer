namespace AiKnowledgeTransfer.Application.Abstractions;

using AiKnowledgeTransfer.Domain.Knowledge;

public sealed record ExtractedKnowledgeItem(
    KnowledgeItemType Type,
    string Title,
    string Summary);
