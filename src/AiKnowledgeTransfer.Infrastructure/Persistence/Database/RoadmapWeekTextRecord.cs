namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class RoadmapWeekTextRecord
{
    public Guid Id { get; set; }

    public Guid RoadmapWeekId { get; set; }

    public RoadmapWeekRecord? RoadmapWeek { get; set; }

    public string Category { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string Text { get; set; } = string.Empty;
}
