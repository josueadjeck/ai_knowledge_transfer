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
        Assert.All(chunks, chunk => Assert.Contains("Document text", chunk.SourceReference, StringComparison.Ordinal));
        Assert.All(chunks, chunk => Assert.Equal("ReviewShortText", chunk.QualityStatus));
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
        Assert.Contains("characters 1200-1300", chunks.Last().SourceReference, StringComparison.Ordinal);
        Assert.All(chunks, chunk => Assert.Equal("UsableText", chunk.QualityStatus));
    }

    [Fact]
    public void SplitIntoChunks_uses_custom_source_reference()
    {
        var chunks = DocumentChunker.SplitIntoChunks("System overview", "Word document body");

        Assert.Single(chunks);
        Assert.Equal("Word document body, characters 0-15", chunks.Single().SourceReference);
        Assert.Equal("ReviewShortText", chunks.Single().QualityStatus);
    }

    [Fact]
    public void SplitIntoChunks_marks_regular_chunks_as_usable()
    {
        var chunks = DocumentChunker.SplitIntoChunks("This paragraph has enough technical context to be reviewed as usable source text.");

        Assert.Single(chunks);
        Assert.Equal("UsableText", chunks.Single().QualityStatus);
    }
}
