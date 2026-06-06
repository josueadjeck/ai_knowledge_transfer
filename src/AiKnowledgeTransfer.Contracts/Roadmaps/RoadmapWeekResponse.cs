namespace AiKnowledgeTransfer.Contracts.Roadmaps;

public sealed record RoadmapWeekResponse(
    int WeekNumber,
    string Theme,
    IReadOnlyCollection<string> LearningGoals,
    IReadOnlyCollection<string> Exercises,
    IReadOnlyCollection<string> AcceptanceCriteria,
    IReadOnlyCollection<string> ReviewNotes);
