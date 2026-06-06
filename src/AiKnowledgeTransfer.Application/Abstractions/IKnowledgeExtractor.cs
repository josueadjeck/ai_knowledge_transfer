namespace AiKnowledgeTransfer.Application.Abstractions;

using AiKnowledgeTransfer.Domain.Documents;

public interface IKnowledgeExtractor
{
    Task<KnowledgeExtractionResult> ExtractAsync(
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken);
}
