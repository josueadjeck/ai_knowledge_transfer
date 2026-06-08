namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class KnowledgeReviewHistoryRecord
{
    public Guid Id { get; set; }

    public Guid KnowledgeItemId { get; set; }

    public KnowledgeItemRecord? KnowledgeItem { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ReviewStatus { get; set; } = string.Empty;

    public string QualityStatus { get; set; } = string.Empty;

    public string Reviewer { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
