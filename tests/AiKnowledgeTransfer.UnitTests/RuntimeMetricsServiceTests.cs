namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Operations;

public sealed class RuntimeMetricsServiceTests
{
    [Fact]
    public void GetSnapshot_reports_runtime_process_metrics()
    {
        var service = new RuntimeMetricsService();

        var metrics = service.GetSnapshot("TestService");

        Assert.Equal("TestService", metrics.Service);
        Assert.True(metrics.UptimeSeconds >= 0);
        Assert.True(metrics.ProcessId > 0);
        Assert.True(metrics.WorkingSetBytes > 0);
        Assert.True(metrics.GcMemoryBytes >= 0);
        Assert.True(metrics.ThreadCount > 0);
        Assert.True(metrics.ProcessorCount > 0);
        Assert.False(string.IsNullOrWhiteSpace(metrics.MachineName));
        Assert.False(string.IsNullOrWhiteSpace(metrics.RuntimeVersion));
    }
}
