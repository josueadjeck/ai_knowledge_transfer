namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class RoadmapWeekRecord
{
    public Guid Id { get; set; }

    public Guid RoadmapId { get; set; }

    public RoadmapRecord? Roadmap { get; set; }

    public int WeekNumber { get; set; }

    public string Theme { get; set; } = string.Empty;

    public List<RoadmapWeekTextRecord> TextItems { get; set; } = [];
}
