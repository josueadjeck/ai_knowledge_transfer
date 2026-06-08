namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Infrastructure.Knowledge;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class FallbackKnowledgeExtractorTests
{
    [Fact]
    public async Task ExtractAsync_marks_fallback_when_primary_returns_no_items()
    {
        var extractor = new FallbackKnowledgeExtractor(
            new StubExtractor(new KnowledgeExtractionResult([], "OpenAI", Detail: "Model: test")),
            new StubExtractor(new KnowledgeExtractionResult(
                [new ExtractedKnowledgeItem(KnowledgeItemType.GlossaryTerm, "CTU", "Candidate.")],
                "Heuristic",
                Detail: "Local rule-based extractor.")),
            NullLogger<FallbackKnowledgeExtractor>.Instance);

        var result = await extractor.ExtractAsync(
            [new DocumentChunk(1, "CTU communication", 0, 17)],
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("CTU", item.Title);
        Assert.Equal("Heuristic", result.ProviderName);
        Assert.True(result.UsedFallback);
        Assert.Contains("returned no items", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_preserves_primary_provider_when_items_are_returned()
    {
        var extractor = new FallbackKnowledgeExtractor(
            new StubExtractor(new KnowledgeExtractionResult(
                [new ExtractedKnowledgeItem(KnowledgeItemType.Component, "PLC", "Controller.")],
                "OpenAI",
                Detail: "Model: test")),
            new StubExtractor(new KnowledgeExtractionResult([], "Heuristic")),
            NullLogger<FallbackKnowledgeExtractor>.Instance);

        var result = await extractor.ExtractAsync(
            [new DocumentChunk(1, "PLC", 0, 3)],
            CancellationToken.None);

        Assert.Equal("OpenAI", result.ProviderName);
        Assert.False(result.UsedFallback);
        Assert.Equal("Model: test", result.Detail);
    }

    private sealed class StubExtractor(KnowledgeExtractionResult result) : IKnowledgeExtractor
    {
        public Task<KnowledgeExtractionResult> ExtractAsync(
            IReadOnlyCollection<DocumentChunk> chunks,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
