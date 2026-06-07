using AiKnowledgeTransfer.Infrastructure.Parsing;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class DocumentChunkerTests
{
    [Fact]
    public void SplitIntoChunks_splits_text_by_paragraphs()
    {
        var chunks = DocumentChunker.SplitIntoChunks("""
            System overview

            Deployment workflow
            """);

        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2], chunks.Select(chunk => chunk.ChunkNumber));
        Assert.Contains(chunks, chunk => chunk.Text == "System overview");
        Assert.Contains(chunks, chunk => chunk.Text == "Deployment workflow");
    }

    [Fact]
    public void SplitIntoChunks_splits_large_paragraphs()
    {
        var text = new string('A', 1300);

        var chunks = DocumentChunker.SplitIntoChunks(text);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(1200, chunks.First().Text.Length);
        Assert.Equal(100, chunks.Last().Text.Length);
        Assert.Equal(0, chunks.First().StartCharacter);
        Assert.Equal(1200, chunks.Last().StartCharacter);
    }
}
