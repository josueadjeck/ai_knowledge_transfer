namespace AiKnowledgeTransfer.Domain.Roadmaps;

public sealed class OnboardingRoadmap
{
    private readonly List<RoadmapWeek> _weeks;

    private OnboardingRoadmap(
        Guid id,
        string targetRole,
        int durationInWeeks,
        DateTimeOffset createdAt,
        RoadmapStatus status,
        IEnumerable<RoadmapWeek> weeks)
    {
        Id = id;
        TargetRole = targetRole;
        DurationInWeeks = durationInWeeks;
        CreatedAt = createdAt;
        Status = status;
        _weeks = weeks.ToList();
    }

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

    public void UpdateWeeks(IEnumerable<RoadmapWeek> weeks)
    {
        var updatedWeeks = weeks
            .OrderBy(week => week.WeekNumber)
            .ToList();

        if (updatedWeeks.Count != DurationInWeeks)
        {
            throw new ArgumentException("Week count must match duration.", nameof(weeks));
        }

        if (updatedWeeks.Select(week => week.WeekNumber).Distinct().Count() != updatedWeeks.Count)
        {
            throw new ArgumentException("Week numbers must be unique.", nameof(weeks));
        }

        if (updatedWeeks.Any(week => week.WeekNumber <= 0))
        {
            throw new ArgumentException("Week numbers must be greater than zero.", nameof(weeks));
        }

        if (!updatedWeeks.Select(week => week.WeekNumber).SequenceEqual(Enumerable.Range(1, DurationInWeeks)))
        {
            throw new ArgumentException("Week numbers must match the roadmap duration.", nameof(weeks));
        }

        _weeks.Clear();
        _weeks.AddRange(updatedWeeks);
    }

    public static OnboardingRoadmap Rehydrate(
        Guid id,
        string targetRole,
        int durationInWeeks,
        DateTimeOffset createdAt,
        RoadmapStatus status,
        IEnumerable<RoadmapWeek> weeks)
    {
        return new OnboardingRoadmap(
            id,
            targetRole,
            durationInWeeks,
            createdAt,
            status,
            weeks);
    }
}
