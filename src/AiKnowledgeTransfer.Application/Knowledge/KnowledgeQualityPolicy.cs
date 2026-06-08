namespace AiKnowledgeTransfer.Application.Knowledge;

using AiKnowledgeTransfer.Domain.Knowledge;

public static class KnowledgeQualityPolicy
{
    public const string NeedsClarification = "NeedsClarification";
    public const string RejectedSource = "RejectedSource";
    public const string Verified = "Verified";

    public static bool IsFinal(KnowledgeItem item)
    {
        return item.ReviewStatus == KnowledgeReviewStatus.Approved
            && item.ExtractionQuality.Equals(Verified, StringComparison.Ordinal);
    }

    public static string ToReviewRisk(KnowledgeItem item)
    {
        return $"{item.Title} ({item.ReviewStatus}, {item.ExtractionQuality}) muss vor finaler Nutzung geprueft werden.";
    }
}
