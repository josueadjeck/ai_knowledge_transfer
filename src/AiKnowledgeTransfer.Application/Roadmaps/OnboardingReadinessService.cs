namespace AiKnowledgeTransfer.Application.Roadmaps;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed class OnboardingReadinessService(IProjectRepository projects)
{
    public async Task<OnboardingReadinessResponse?> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var finalKnowledgeCount = project.KnowledgeItems.Count(KnowledgeQualityPolicy.IsFinal);
        var reviewRiskCount = project.KnowledgeItems.Count(item => !KnowledgeQualityPolicy.IsFinal(item));
        var latestRoadmap = project.Roadmaps.OrderByDescending(roadmap => roadmap.CreatedAt).FirstOrDefault();
        var hasRoadmap = latestRoadmap is not null;
        var canStart = hasRoadmap && finalKnowledgeCount > 0;

        return new OnboardingReadinessResponse(
            project.Id,
            DetermineStatus(canStart, hasRoadmap, finalKnowledgeCount),
            canStart,
            finalKnowledgeCount,
            reviewRiskCount,
            project.Roadmaps.Count,
            latestRoadmap?.TargetRole,
            BuildRecommendations(canStart, hasRoadmap, finalKnowledgeCount, reviewRiskCount));
    }

    private static string DetermineStatus(bool canStart, bool hasRoadmap, int finalKnowledgeCount)
    {
        if (canStart)
        {
            return "ReadyForPilot";
        }

        if (!hasRoadmap && finalKnowledgeCount > 0)
        {
            return "NeedsRoadmap";
        }

        if (hasRoadmap)
        {
            return "NeedsKnowledgeReview";
        }

        return "NotReady";
    }

    private static IReadOnlyCollection<string> BuildRecommendations(
        bool canStart,
        bool hasRoadmap,
        int finalKnowledgeCount,
        int reviewRiskCount)
    {
        var recommendations = new List<string>();

        if (!hasRoadmap)
        {
            recommendations.Add("Roadmap fuer die Zielrolle erzeugen.");
        }

        if (finalKnowledgeCount == 0)
        {
            recommendations.Add("Mindestens ein Wissenselement freigeben und als Verified markieren.");
        }

        if (reviewRiskCount > 0)
        {
            recommendations.Add($"{reviewRiskCount} Wissenselemente bleiben Review-Risiken und sollten vor breiter Nutzung geprueft werden.");
        }

        if (canStart)
        {
            recommendations.Add("Betreuten Pilot-Onboarding-Start mit Senior-Review einplanen.");
        }

        return recommendations;
    }
}
