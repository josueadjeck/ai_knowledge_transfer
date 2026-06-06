namespace AiKnowledgeTransfer.Application.Traceability;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Traceability;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;

public sealed class TraceabilityService(IProjectRepository projects)
{
    public async Task<TraceabilityMatrixResponse?> GetMatrixAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        return new TraceabilityMatrixResponse(
            project.Id,
            project.Name,
            DateTimeOffset.UtcNow,
            BuildRows(project));
    }

    private static IReadOnlyCollection<TraceabilityRowResponse> BuildRows(KnowledgeProject project)
    {
        var rows = new List<TraceabilityRowResponse>();

        foreach (var document in project.Documents.OrderBy(document => document.FileName).ThenBy(document => document.VersionNumber))
        {
            var linkedKnowledgeItems = project.KnowledgeItems
                .Where(item => item.SourceDocumentId == document.Id)
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Title)
                .ToArray();

            if (linkedKnowledgeItems.Length == 0)
            {
                rows.Add(new TraceabilityRowResponse(
                    document.Id,
                    document.FileName,
                    document.Status.ToString(),
                    document.Chunks.Count,
                    KnowledgeItemId: null,
                    KnowledgeType: null,
                    KnowledgeTitle: null,
                    KnowledgeReviewStatus: null,
                    ReviewedBy: null,
                    ReviewedAt: null,
                    RoadmapUsageCount: 0,
                    IncludedInExport: false));

                continue;
            }

            foreach (var item in linkedKnowledgeItems)
            {
                rows.Add(new TraceabilityRowResponse(
                    document.Id,
                    document.FileName,
                    document.Status.ToString(),
                    document.Chunks.Count,
                    item.Id,
                    item.Type.ToString(),
                    item.Title,
                    item.ReviewStatus.ToString(),
                    item.ReviewedBy,
                    item.ReviewedAt,
                    CountRoadmapUsage(project, item),
                    item.ReviewStatus == KnowledgeReviewStatus.Approved));
            }
        }

        return rows;
    }

    private static int CountRoadmapUsage(KnowledgeProject project, KnowledgeItem item)
    {
        return project.Roadmaps
            .SelectMany(roadmap => roadmap.Weeks)
            .Count(week =>
                ContainsTitle(week.LearningGoals, item.Title)
                || ContainsTitle(week.Exercises, item.Title)
                || ContainsTitle(week.ReviewNotes, item.Title));
    }

    private static bool ContainsTitle(IEnumerable<string> values, string title)
    {
        return values.Any(value => value.Contains(title, StringComparison.OrdinalIgnoreCase));
    }
}
