namespace AiKnowledgeTransfer.Application.Roadmaps;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed class OnboardingStartPackageService(
    IProjectRepository projects,
    OnboardingReadinessService readinessService)
{
    public async Task<OnboardingStartPackageResponse?> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var readiness = await readinessService.GetAsync(projectId, cancellationToken);
        if (readiness is null)
        {
            return null;
        }

        var latestRoadmap = project.Roadmaps.OrderByDescending(roadmap => roadmap.CreatedAt).FirstOrDefault();
        var firstWeeks = latestRoadmap is null
            ? Array.Empty<RoadmapWeekResponse>()
            : RoadmapMapper.ToResponse(latestRoadmap).Weeks
                .OrderBy(week => week.WeekNumber)
                .Take(2)
                .ToArray();

        return new OnboardingStartPackageResponse(
            project.Id,
            project.Name,
            readiness.Status,
            readiness.CanStartOnboarding,
            latestRoadmap?.TargetRole,
            firstWeeks,
            BuildStarterTasks(readiness.CanStartOnboarding, firstWeeks),
            BuildReviewWarnings(readiness.ReviewRiskCount, project.KnowledgeItems
                .Where(item => !KnowledgeQualityPolicy.IsFinal(item))
                .Select(KnowledgeQualityPolicy.ToReviewRisk)
                .Take(5)
                .ToArray()));
    }

    private static IReadOnlyCollection<string> BuildStarterTasks(
        bool canStart,
        IReadOnlyCollection<RoadmapWeekResponse> firstWeeks)
    {
        if (!canStart)
        {
            return
            [
                "Readiness-Empfehlungen abarbeiten, bevor ein Pilot-Onboarding startet."
            ];
        }

        return firstWeeks
            .SelectMany(week => week.LearningGoals.Take(2).Concat(week.Exercises.Take(1)))
            .DefaultIfEmpty("Kickoff mit Senior Engineer und Projektkontext planen.")
            .Take(6)
            .ToArray();
    }

    private static IReadOnlyCollection<string> BuildReviewWarnings(
        int reviewRiskCount,
        IReadOnlyCollection<string> reviewRisks)
    {
        if (reviewRiskCount == 0)
        {
            return [];
        }

        return reviewRisks.Count == 0
            ? [$"{reviewRiskCount} Review-Risiken vor breiter Nutzung pruefen."]
            : reviewRisks;
    }
}
