namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class RoadmapRecord
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public ProjectRecord? Project { get; set; }

    public string TargetRole { get; set; } = string.Empty;

    public int DurationInWeeks { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<RoadmapWeekRecord> Weeks { get; set; } = [];
}
