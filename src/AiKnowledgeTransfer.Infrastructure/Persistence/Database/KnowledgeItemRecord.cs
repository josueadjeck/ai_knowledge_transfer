namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class KnowledgeItemRecord
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public ProjectRecord? Project { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public Guid? SourceDocumentId { get; set; }

    public string ReviewStatus { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public string? ReviewComment { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }
}
