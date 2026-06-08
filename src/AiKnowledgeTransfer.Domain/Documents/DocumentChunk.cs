namespace AiKnowledgeTransfer.Domain.Documents;

public sealed class DocumentChunk
{
    private DocumentChunk(
        Guid id,
        int chunkNumber,
        string text,
        int startCharacter,
        int endCharacter,
        string sourceReference,
        DateTimeOffset createdAt)
    {
        Id = id;
        ChunkNumber = chunkNumber;
        Text = text;
        StartCharacter = startCharacter;
        EndCharacter = endCharacter;
        SourceReference = string.IsNullOrWhiteSpace(sourceReference)
            ? "Imported legacy chunk"
            : sourceReference.Trim();
        CreatedAt = createdAt;
    }

    public DocumentChunk(
        int chunkNumber,
        string text,
        int startCharacter,
        int endCharacter,
        string sourceReference = "Document text")
    {
        if (chunkNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkNumber), "Chunk number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Chunk text must not be empty.", nameof(text));
        }

        if (startCharacter < 0 || endCharacter < startCharacter)
        {
            throw new ArgumentException("Invalid character range.");
        }

        Id = Guid.NewGuid();
        ChunkNumber = chunkNumber;
        Text = text.Trim();
        StartCharacter = startCharacter;
        EndCharacter = endCharacter;
        SourceReference = string.IsNullOrWhiteSpace(sourceReference)
            ? "Document text"
            : sourceReference.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public int ChunkNumber { get; }

    public string Text { get; }

    public int StartCharacter { get; }

    public int EndCharacter { get; }

    public string SourceReference { get; }

    public DateTimeOffset CreatedAt { get; }

    public static DocumentChunk Rehydrate(
        Guid id,
        int chunkNumber,
        string text,
        int startCharacter,
        int endCharacter,
        DateTimeOffset createdAt,
        string sourceReference = "Imported legacy chunk")
    {
        return new DocumentChunk(
            id,
            chunkNumber,
            text,
            startCharacter,
            endCharacter,
            sourceReference,
            createdAt);
    }
}
