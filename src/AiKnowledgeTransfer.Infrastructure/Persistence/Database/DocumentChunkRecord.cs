namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class DocumentChunkRecord
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public DocumentRecord? Document { get; set; }

    public int ChunkNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public int StartCharacter { get; set; }

    public int EndCharacter { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
