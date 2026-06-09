namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Operations;

public sealed class RoadmapReadinessServiceTests
{
    [Fact]
    public void GetStatus_reports_completed_roadmap_audit_with_remaining_notes()
    {
        var service = new RoadmapReadinessService();

        var readiness = service.GetStatus();

        Assert.Equal("Complete", readiness.Status);
        Assert.Equal(10, readiness.PhaseCount);
        Assert.True(readiness.ReadyPhaseCount > 0);
        Assert.Equal(0, readiness.NeedsWorkPhaseCount);
        Assert.Contains(readiness.Phases, phase =>
            phase.Phase == 5
            && phase.Status == "Ready"
            && phase.Evidence.Any(evidence => evidence.Contains("Editable roadmap", StringComparison.Ordinal)));
        Assert.Contains(readiness.Phases, phase =>
            phase.Phase == 6
            && phase.Status == "Ready"
            && phase.Evidence.Any(evidence => evidence.Contains("version comparison", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(readiness.Phases, phase =>
            phase.Phase == 9
            && phase.Status == "ReadyWithNotes"
            && phase.Gaps.Any(gap => gap.Contains("future enterprise", StringComparison.OrdinalIgnoreCase)));
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
