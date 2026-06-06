namespace AiKnowledgeTransfer.Domain.Knowledge;

public sealed class KnowledgeItem
{
    public KnowledgeItem(KnowledgeItemType type, string title, string summary, Guid? sourceDocumentId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title must not be empty.", nameof(title));
        }

        Id = Guid.NewGuid();
        Type = type;
        Title = title.Trim();
        Summary = summary.Trim();
        SourceDocumentId = sourceDocumentId;
        ReviewStatus = KnowledgeReviewStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public KnowledgeItemType Type { get; }

    public string Title { get; }

    public string Summary { get; }

    public Guid? SourceDocumentId { get; }

    public KnowledgeReviewStatus ReviewStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void Approve()
    {
        ReviewStatus = KnowledgeReviewStatus.Approved;
    }
}
