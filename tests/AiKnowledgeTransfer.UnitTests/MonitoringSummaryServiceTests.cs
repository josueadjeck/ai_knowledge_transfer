namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Operations;
using AiKnowledgeTransfer.Application.Security;

public sealed class MonitoringSummaryServiceTests
{
    [Fact]
    public void GetSummary_reports_needs_review_when_manual_release_checks_are_open()
    {
        var service = CreateService(aiApiKeyConfigured: true);

        var summary = service.GetSummary("TestService");

        Assert.Equal("TestService", summary.Service);
        Assert.Equal("ok", summary.HealthStatus);
        Assert.Equal("NeedsReview", summary.ReleaseReadinessStatus);
        Assert.Equal("needsReview", summary.Status);
        Assert.True(summary.ManualReviewCount > 0);
        Assert.Contains(summary.Signals, signal => signal.Status == "Manual");
        Assert.True(summary.UptimeSeconds >= 0);
        Assert.True(summary.WorkingSetBytes > 0);
    }

    [Fact]
    public void GetSummary_reports_degraded_when_health_has_errors()
    {
        var service = CreateService(
            aiApiKeyConfigured: true,
            authentication: new AuthenticationOptions("Oidc", null, null, "roles"));

        var summary = service.GetSummary("TestService");

        Assert.Equal("degraded", summary.Status);
        Assert.Equal("degraded", summary.HealthStatus);
        Assert.True(summary.ErrorCount > 0);
        Assert.Contains(summary.Signals, signal =>
            signal.Name == "authentication"
            && signal.Status == "Error");
    }

    private static MonitoringSummaryService CreateService(
        bool aiApiKeyConfigured,
        AuthenticationOptions? authentication = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-monitoring-tests", Guid.NewGuid().ToString("N"));
        authentication ??= new AuthenticationOptions("Demo", null, null, "role");
        var health = new OperationalHealthService(new OperationalHealthOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            "Json",
            null,
            null,
            "EnsureCreated",
            "OpenAI",
            "gpt-5.4-mini",
            "https://api.openai.com/v1/",
            aiApiKeyConfigured,
            MaxAnalysisChunksPerDocument: 250,
            MaxAnalysisExtractedCharacters: 500_000,
            MaxExtractionChunks: 80,
            MaxExtractionChunkCharacters: 120_000,
            authentication.Mode,
            authentication.Authority,
            authentication.ClientId,
            authentication.RoleClaimType,
            "SingleTenant",
            "tenant_id",
            "default"));
        var backups = new PersistenceBackupService(new PersistenceBackupOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            Path.Combine(root, "backups")));
        backups.CreateAsync(CancellationToken.None).GetAwaiter().GetResult();
        var readiness = new ReleaseReadinessService(health, backups, authentication);
        var metrics = new RuntimeMetricsService();

        return new MonitoringSummaryService(health, readiness, metrics);
    }
}
