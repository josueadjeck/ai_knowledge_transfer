namespace AiKnowledgeTransfer.Infrastructure.Knowledge;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Documents;
using Microsoft.Extensions.Logging;

public sealed class FallbackKnowledgeExtractor(
    IKnowledgeExtractor primary,
    IKnowledgeExtractor fallback,
    ILogger<FallbackKnowledgeExtractor> logger) : IKnowledgeExtractor
{
    public async Task<KnowledgeExtractionResult> ExtractAsync(
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await primary.ExtractAsync(chunks, cancellationToken);
            if (result.Items.Count > 0)
            {
                return result;
            }

            logger.LogWarning("Primary knowledge extractor returned no items; using fallback extractor.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Primary knowledge extractor failed; using fallback extractor.");
        }

        return await fallback.ExtractAsync(chunks, cancellationToken);
    }
}
