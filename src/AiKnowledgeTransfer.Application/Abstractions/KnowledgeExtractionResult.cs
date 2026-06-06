namespace AiKnowledgeTransfer.Application.Abstractions;

using AiKnowledgeTransfer.Domain.Knowledge;

public sealed record KnowledgeExtractionResult(
    IReadOnlyCollection<ExtractedKnowledgeItem> Items);
