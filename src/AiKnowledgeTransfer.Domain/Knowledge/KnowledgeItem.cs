namespace AiKnowledgeTransfer.Domain.Knowledge;

public sealed class KnowledgeItem
{
    private readonly List<KnowledgeReviewHistoryEntry> _reviewHistory = [];

    private KnowledgeItem(
        Guid id,
        KnowledgeItemType type,
        string title,
        string summary,
        Guid? sourceDocumentId,
        int? sourceChunkNumber,
        string extractionProvider,
        string? extractionModel,
        bool extractionUsedFallback,
        string extractionQuality,
        KnowledgeReviewStatus reviewStatus,
        DateTimeOffset createdAt,
        string? reviewedBy,
        string? reviewComment,
        DateTimeOffset? reviewedAt,
        IEnumerable<KnowledgeReviewHistoryEntry>? reviewHistory = null)
    {
        Id = id;
        Type = type;
        Title = title;
        Summary = summary;
        SourceDocumentId = sourceDocumentId;
        SourceChunkNumber = sourceChunkNumber;
        ExtractionProvider = extractionProvider;
        ExtractionModel = extractionModel;
        ExtractionUsedFallback = extractionUsedFallback;
        ExtractionQuality = extractionQuality;
        ReviewStatus = reviewStatus;
        CreatedAt = createdAt;
        ReviewedBy = reviewedBy;
        ReviewComment = reviewComment;
        ReviewedAt = reviewedAt;
        _reviewHistory.AddRange(reviewHistory ?? []);
    }

    public KnowledgeItem(
        KnowledgeItemType type,
        string title,
        string summary,
        Guid? sourceDocumentId,
        int? sourceChunkNumber = null,
        string extractionProvider = "Manual",
        string? extractionModel = null,
        bool extractionUsedFallback = false,
        string extractionQuality = "HumanSeeded")
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
        SourceChunkNumber = sourceChunkNumber;
        ExtractionProvider = RequireText(extractionProvider, nameof(extractionProvider));
        ExtractionModel = string.IsNullOrWhiteSpace(extractionModel) ? null : extractionModel.Trim();
        ExtractionUsedFallback = extractionUsedFallback;
        ExtractionQuality = RequireText(extractionQuality, nameof(extractionQuality));
        ReviewStatus = KnowledgeReviewStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public KnowledgeItemType Type { get; }

    public string Title { get; }

    public string Summary { get; }

    public Guid? SourceDocumentId { get; }

    public int? SourceChunkNumber { get; }

    public string ExtractionProvider { get; }

    public string? ExtractionModel { get; }

    public bool ExtractionUsedFallback { get; }

    public string ExtractionQuality { get; private set; }

    public KnowledgeReviewStatus ReviewStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public string? ReviewedBy { get; private set; }

    public string? ReviewComment { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public IReadOnlyCollection<KnowledgeReviewHistoryEntry> ReviewHistory => _reviewHistory;

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

    public void UpdateExtractionQuality(string qualityStatus)
    {
        ExtractionQuality = RequireText(qualityStatus, nameof(qualityStatus));
    }

    public void RecordReviewHistory(string action)
    {
        if (string.IsNullOrWhiteSpace(ReviewedBy))
        {
            return;
        }

        _reviewHistory.Add(KnowledgeReviewHistoryEntry.Create(
            action,
            ReviewStatus,
            ExtractionQuality,
            ReviewedBy,
            ReviewComment ?? string.Empty));
    }

    private static string RequireReviewer(string reviewer)
    {
        return RequireText(reviewer, nameof(reviewer));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }

    public static KnowledgeItem Rehydrate(
        Guid id,
        KnowledgeItemType type,
        string title,
        string summary,
        Guid? sourceDocumentId,
        int? sourceChunkNumber,
        string extractionProvider,
        string? extractionModel,
        bool extractionUsedFallback,
        string extractionQuality,
        KnowledgeReviewStatus reviewStatus,
        DateTimeOffset createdAt,
        string? reviewedBy,
        string? reviewComment,
        DateTimeOffset? reviewedAt,
        IEnumerable<KnowledgeReviewHistoryEntry>? reviewHistory = null)
    {
        return new KnowledgeItem(
            id,
            type,
            title,
            summary,
            sourceDocumentId,
            sourceChunkNumber,
            extractionProvider,
            extractionModel,
            extractionUsedFallback,
            extractionQuality,
            reviewStatus,
            createdAt,
            reviewedBy,
            reviewComment,
            reviewedAt,
            reviewHistory);
    }
}
