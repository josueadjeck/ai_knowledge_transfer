namespace AiKnowledgeTransfer.Domain.Knowledge;

public sealed class KnowledgeItem
{
    private KnowledgeItem(
        Guid id,
        KnowledgeItemType type,
        string title,
        string summary,
        Guid? sourceDocumentId,
        KnowledgeReviewStatus reviewStatus,
        DateTimeOffset createdAt,
        string? reviewedBy,
        string? reviewComment,
        DateTimeOffset? reviewedAt)
    {
        Id = id;
        Type = type;
        Title = title;
        Summary = summary;
        SourceDocumentId = sourceDocumentId;
        ReviewStatus = reviewStatus;
        CreatedAt = createdAt;
        ReviewedBy = reviewedBy;
        ReviewComment = reviewComment;
        ReviewedAt = reviewedAt;
    }

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

    public string? ReviewedBy { get; private set; }

    public string? ReviewComment { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public void SubmitForReview(string reviewer, string comment)
    {
        ReviewStatus = KnowledgeReviewStatus.InReview;
        ReviewedBy = RequireReviewer(reviewer);
        ReviewComment = comment.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Approve(string reviewer, string comment)
    {
        ReviewStatus = KnowledgeReviewStatus.Approved;
        ReviewedBy = RequireReviewer(reviewer);
        ReviewComment = comment.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string reviewer, string comment)
    {
        ReviewStatus = KnowledgeReviewStatus.Rejected;
        ReviewedBy = RequireReviewer(reviewer);
        ReviewComment = comment.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    private static string RequireReviewer(string reviewer)
    {
        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new ArgumentException("Reviewer must not be empty.", nameof(reviewer));
        }

        return reviewer.Trim();
    }

    public static KnowledgeItem Rehydrate(
        Guid id,
        KnowledgeItemType type,
        string title,
        string summary,
        Guid? sourceDocumentId,
        KnowledgeReviewStatus reviewStatus,
        DateTimeOffset createdAt,
        string? reviewedBy,
        string? reviewComment,
        DateTimeOffset? reviewedAt)
    {
        return new KnowledgeItem(
            id,
            type,
            title,
            summary,
            sourceDocumentId,
            reviewStatus,
            createdAt,
            reviewedBy,
            reviewComment,
            reviewedAt);
    }
}
