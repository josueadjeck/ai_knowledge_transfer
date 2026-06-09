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
            "Json",
            null,
            null,
            "OpenAI",
            "gpt-5.4-mini",
            "https://api.openai.com/v1/",
            AiApiKeyConfigured: false,
            "Demo",
            null,
            null,
            "role");
        var service = new OperationalHealthService(options);

        var health = service.GetStatus("TestService");

        Assert.Equal("ok", health.Status);
        Assert.Equal("TestService", health.Service);
        Assert.Contains(health.Components, component => component.Name == "storage" && component.Status == "ok");
        Assert.Contains(health.Components, component => component.Name == "persistence" && component.Metadata["provider"] == "Json");
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
            "Json",
            null,
            null,
            "OpenAI",
            "custom-model",
            "https://example.test/v1/",
            AiApiKeyConfigured: true,
            "Demo",
            null,
            null,
            "role");
        var service = new OperationalHealthService(options);

        var health = service.GetStatus("TestService");

        Assert.Contains(health.Components, component =>
            component.Name == "aiProvider"
            && component.Status == "ok"
            && component.Metadata["model"] == "custom-model");
    }

    [Fact]
    public void GetStatus_reports_database_persistence_configuration()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-health-tests", Guid.NewGuid().ToString("N"));
        var options = new OperationalHealthOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            "Database",
            "Sqlite",
            "Data Source=knowledge-transfer.db",
            "OpenAI",
            "gpt-5.4-mini",
            "https://api.openai.com/v1/",
            AiApiKeyConfigured: false,
            "Demo",
            null,
            null,
            "role");
        var service = new OperationalHealthService(options);

        var health = service.GetStatus("TestService");

        Assert.Contains(health.Components, component =>
            component.Name == "persistence"
            && component.Status == "ok"
            && component.Metadata["databaseProvider"] == "Sqlite"
            && component.Metadata["connectionStringConfigured"] == "True");
        Assert.Contains(health.Components, component => component.Name == "projectStore" && component.Status == "database");
        Assert.Contains(health.Components, component => component.Name == "auditLog" && component.Status == "database");
    }

    [Fact]
    public void GetStatus_reports_oidc_authentication_configuration_errors()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-health-tests", Guid.NewGuid().ToString("N"));
        var options = new OperationalHealthOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            "Json",
            null,
            null,
            "OpenAI",
            "gpt-5.4-mini",
            "https://api.openai.com/v1/",
            AiApiKeyConfigured: false,
            "Oidc",
            null,
            null,
            "roles");
        var service = new OperationalHealthService(options);

        var health = service.GetStatus("TestService");

        Assert.Equal("degraded", health.Status);
        Assert.Contains(health.Components, component =>
            component.Name == "authentication"
            && component.Status == "error"
            && component.Metadata["roleClaimType"] == "roles");
    }
}
