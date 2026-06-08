namespace AiKnowledgeTransfer.Domain.Knowledge;

public sealed record KnowledgeReviewHistoryEntry(
    Guid Id,
    string Action,
    KnowledgeReviewStatus ReviewStatus,
    string QualityStatus,
    string Reviewer,
    string Comment,
    DateTimeOffset CreatedAt)
{
    public static KnowledgeReviewHistoryEntry Create(
        string action,
        KnowledgeReviewStatus reviewStatus,
        string qualityStatus,
        string reviewer,
        string comment)
    {
        return new KnowledgeReviewHistoryEntry(
            Guid.NewGuid(),
            RequireText(action, nameof(action)),
            reviewStatus,
            RequireText(qualityStatus, nameof(qualityStatus)),
            RequireText(reviewer, nameof(reviewer)),
            comment.Trim(),
            DateTimeOffset.UtcNow);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }
}
