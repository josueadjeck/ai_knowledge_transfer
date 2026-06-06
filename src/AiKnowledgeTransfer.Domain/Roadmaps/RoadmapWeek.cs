namespace AiKnowledgeTransfer.Domain.Roadmaps;

public sealed record RoadmapWeek(
    int WeekNumber,
    string Theme,
    IReadOnlyCollection<string> LearningGoals,
    IReadOnlyCollection<string> Exercises,
    IReadOnlyCollection<string> AcceptanceCriteria,
    IReadOnlyCollection<string> ReviewNotes);
