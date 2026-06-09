namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Operations;

public sealed class RoadmapReadinessServiceTests
{
    [Fact]
    public void GetStatus_reports_roadmap_audit_with_remaining_gaps()
    {
        var service = new RoadmapReadinessService();

        var readiness = service.GetStatus();

        Assert.Equal("NeedsWork", readiness.Status);
        Assert.Equal(10, readiness.PhaseCount);
        Assert.True(readiness.ReadyPhaseCount > 0);
        Assert.True(readiness.NeedsWorkPhaseCount > 0);
        Assert.Contains(readiness.Phases, phase =>
            phase.Phase == 5
            && phase.Status == "Ready"
            && phase.Evidence.Any(evidence => evidence.Contains("Editable roadmap", StringComparison.Ordinal)));
        Assert.Contains(readiness.Phases, phase =>
            phase.Phase == 9
            && phase.Gaps.Any(gap => gap.Contains("tenant isolation", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void GetStatus_marks_ready_phases_with_evidence()
    {
        var service = new RoadmapReadinessService();

        var readiness = service.GetStatus();

        Assert.Contains(readiness.Phases, phase =>
            phase.Phase == 4
            && phase.Status == "Ready"
            && phase.Evidence.Any(evidence => evidence.Contains("PDF and Word parsing", StringComparison.Ordinal)));
    }
}
