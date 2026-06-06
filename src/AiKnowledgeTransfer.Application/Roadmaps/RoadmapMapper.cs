namespace AiKnowledgeTransfer.Application.Roadmaps;

using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Domain.Roadmaps;

internal static class RoadmapMapper
{
    public static RoadmapResponse ToResponse(OnboardingRoadmap roadmap)
    {
        return new RoadmapResponse(
            roadmap.Id,
            roadmap.TargetRole,
            roadmap.DurationInWeeks,
            roadmap.Status.ToString(),
            roadmap.CreatedAt,
            roadmap.Weeks.Select(ToResponse).ToArray());
    }

    private static RoadmapWeekResponse ToResponse(RoadmapWeek week)
    {
        return new RoadmapWeekResponse(
            week.WeekNumber,
            week.Theme,
            week.LearningGoals,
            week.Exercises,
            week.AcceptanceCriteria);
    }
}
