namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record UpdateRoadmapWeekRequest(
    int WeekNumber,
    string Theme,
    IReadOnlyCollection<string> LearningGoals,
    IReadOnlyCollection<string> Exercises,
    IReadOnlyCollection<string> AcceptanceCriteria,
    IReadOnlyCollection<string> ReviewNotes);
