using AiKnowledgeTransfer.Application.Diagnostics;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class OperationalHealthServiceTests
{
    [Fact]
    public void GetStatus_reports_storage_and_fallback_ai_provider()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-health-tests", Guid.NewGuid().ToString("N"));
        var options = new OperationalHealthOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            "OpenAI",
            "gpt-5.4-mini",
            "https://api.openai.com/v1/",
            AiApiKeyConfigured: false);
        var service = new OperationalHealthService(options);

        var health = service.GetStatus("TestService");

        Assert.Equal("ok", health.Status);
        Assert.Equal("TestService", health.Service);
        Assert.Contains(health.Components, component => component.Name == "storage" && component.Status == "ok");
        Assert.Contains(health.Components, component => component.Name == "aiProvider" && component.Status == "fallback");
    }

    [Fact]
    public void GetStatus_reports_configured_ai_provider()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-health-tests", Guid.NewGuid().ToString("N"));
        var options = new OperationalHealthOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            "OpenAI",
            "custom-model",
            "https://example.test/v1/",
            AiApiKeyConfigured: true);
        var service = new OperationalHealthService(options);

        var health = service.GetStatus("TestService");

        Assert.Contains(health.Components, component =>
            component.Name == "aiProvider"
            && component.Status == "ok"
            && component.Metadata["model"] == "custom-model");
    }
}
