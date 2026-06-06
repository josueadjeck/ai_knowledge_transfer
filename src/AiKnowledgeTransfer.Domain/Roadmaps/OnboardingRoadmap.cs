namespace AiKnowledgeTransfer.Domain.Roadmaps;

public sealed class OnboardingRoadmap
{
    private readonly List<RoadmapWeek> _weeks;

    public OnboardingRoadmap(string targetRole, int durationInWeeks, IEnumerable<RoadmapWeek> weeks)
    {
        if (string.IsNullOrWhiteSpace(targetRole))
        {
            throw new ArgumentException("Target role must not be empty.", nameof(targetRole));
        }

        if (durationInWeeks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationInWeeks), "Duration must be greater than zero.");
        }

        _weeks = weeks.ToList();
        if (_weeks.Count != durationInWeeks)
        {
            throw new ArgumentException("Week count must match duration.", nameof(weeks));
        }

        Id = Guid.NewGuid();
        TargetRole = targetRole.Trim();
        DurationInWeeks = durationInWeeks;
        CreatedAt = DateTimeOffset.UtcNow;
        Status = RoadmapStatus.Draft;
    }

    public Guid Id { get; }

    public string TargetRole { get; }

    public int DurationInWeeks { get; }

    public DateTimeOffset CreatedAt { get; }

    public RoadmapStatus Status { get; private set; }

    public IReadOnlyCollection<RoadmapWeek> Weeks => _weeks;
}
