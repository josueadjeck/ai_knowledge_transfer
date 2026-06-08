namespace AiKnowledgeTransfer.Application.Knowledge;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Knowledge;

public sealed class KnowledgeReviewSummaryService(IProjectRepository projects)
{
    public async Task<KnowledgeReviewSummaryResponse?> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var items = project.KnowledgeItems;
        return new KnowledgeReviewSummaryResponse(
            project.Id,
            items.Count,
            items.Count(KnowledgeQualityPolicy.IsFinal),
            Group(items, item => item.ReviewStatus.ToString()),
            Group(items, item => item.ExtractionQuality),
            Group(items, item => item.Type.ToString()));
    }

    private static IReadOnlyCollection<KnowledgeReviewSummaryGroupResponse> Group(
        IEnumerable<KnowledgeItem> items,
        Func<KnowledgeItem, string?> selector)
    {
        return items
            .Select(item => selector(item))
            .Select(value => string.IsNullOrWhiteSpace(value) ? "Unspecified" : value)
            .GroupBy(value => value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new KnowledgeReviewSummaryGroupResponse(group.Key, group.Count()))
            .ToArray();
    }
}
