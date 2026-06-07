namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class DocumentRecord
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public ProjectRecord? Project { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public int VersionNumber { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<DocumentChunkRecord> Chunks { get; set; } = [];
}
