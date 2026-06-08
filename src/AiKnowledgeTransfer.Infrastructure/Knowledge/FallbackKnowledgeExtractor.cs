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
            return await ExtractFallbackAsync(chunks, "Primary extractor returned no items.", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Primary knowledge extractor failed; using fallback extractor.");
            return await ExtractFallbackAsync(chunks, $"Primary extractor failed: {exception.GetType().Name}.", cancellationToken);
        }
    }

    private async Task<KnowledgeExtractionResult> ExtractFallbackAsync(
        IReadOnlyCollection<DocumentChunk> chunks,
        string reason,
        CancellationToken cancellationToken)
    {
        var result = await fallback.ExtractAsync(chunks, cancellationToken);
        var detail = string.IsNullOrWhiteSpace(result.Detail)
            ? reason
            : $"{reason} {result.Detail}";

        return result with
        {
            UsedFallback = true,
            Detail = detail
        };
    }
}
