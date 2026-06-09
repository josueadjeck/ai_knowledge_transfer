namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Operations;
using AiKnowledgeTransfer.Application.Security;

public sealed class ReleaseReadinessServiceTests
{
    [Fact]
    public void GetStatus_blocks_release_without_backup()
    {
        var root = CreateRoot();
        var service = CreateService(root, aiApiKeyConfigured: true);

        var readiness = service.GetStatus("TestService");

        Assert.Equal("Blocked", readiness.Status);
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Backup available"
            && check.Required
            && check.Status == "Fail");
    }

    [Fact]
    public async Task GetStatus_reports_needs_review_when_backup_exists_and_manual_checks_remain()
    {
        var root = CreateRoot();
        var backupOptions = CreateBackupOptions(root);
        var backupService = new PersistenceBackupService(backupOptions);
        Directory.CreateDirectory(backupOptions.UploadStoragePath);
        await File.WriteAllTextAsync(backupOptions.ProjectStorePath, """{"projects":[]}""");
        await File.WriteAllTextAsync(backupOptions.AuditLogPath, """{"events":[]}""");
        await backupService.CreateAsync(CancellationToken.None);

        var readiness = CreateService(root, aiApiKeyConfigured: false).GetStatus("TestService");

        Assert.Equal("NeedsReview", readiness.Status);
        Assert.Contains(readiness.Checks, check => check.Name == "Backup available" && check.Status == "Pass");
        Assert.Contains(readiness.Checks, check => check.Name == "AI provider" && check.Status == "Warning");
        Assert.Contains(readiness.Checks, check => check.Name == "CI security gates" && check.Status == "Manual");
        Assert.Contains(readiness.Checks, check => check.Name == "Security inventory" && check.Status == "Manual");
        Assert.Contains(readiness.Checks, check => check.Name == "Container image inventory" && check.Status == "Manual");
        Assert.Contains(readiness.Checks, check => check.Name == "Security and data protection review" && check.Status == "Manual");
        Assert.Contains(readiness.Checks, check => check.Name == "Secret management review" && check.Status == "Manual");
    }

    [Fact]
    public void GetStatus_requires_security_inventory_review()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Security inventory"
            && check.Required
            && check.Status == "Manual"
            && check.Evidence.Contains("security-inventory", StringComparison.Ordinal)
            && check.Evidence.Contains("sbom.spdx.json", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_requires_container_image_inventory_review()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Container image inventory"
            && check.Required
            && check.Status == "Manual"
            && check.Evidence.Contains("container-policy-scan.json", StringComparison.Ordinal)
            && check.Evidence.Contains("container-images.json", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_requires_security_and_data_protection_review()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Security and data protection review"
            && check.Required
            && check.Status == "Manual"
            && check.Evidence.Contains("docs/security/security-concept.md", StringComparison.Ordinal)
            && check.Evidence.Contains("docs/security/data-protection-concept.md", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_requires_secret_management_review()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Secret management review"
            && check.Required
            && check.Status == "Manual"
            && check.Evidence.Contains("docs/security/secret-management.md", StringComparison.Ordinal)
            && check.Evidence.Contains("OPENAI_API_KEY", StringComparison.Ordinal)
            && check.Evidence.Contains("AKT_DB_CONNECTION_STRING", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_blocks_release_when_oidc_configuration_is_incomplete()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            new AuthenticationOptions("Oidc", null, null, "roles"))
            .GetStatus("TestService");

        Assert.Equal("Blocked", readiness.Status);
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Authentication configuration"
            && check.Status == "Fail");
    }

    [Fact]
    public void GetStatus_warns_when_demo_authentication_is_active()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Authentication configuration"
            && check.Status == "Warning"
            && check.Detail.Contains("Demo authentication mode", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_passes_when_oidc_authentication_is_configured()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            new AuthenticationOptions("Oidc", "https://login.example.test/tenant/v2.0", "client-id", "roles"))
            .GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Authentication configuration"
            && check.Status == "Pass"
            && check.Detail.Contains("OIDC authentication settings", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_warns_when_single_tenant_mode_is_active()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Tenancy configuration"
            && check.Status == "Warning"
            && check.Detail.Contains("Single-tenant MVP mode", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_passes_when_multi_tenant_mode_is_configured()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            tenancy: new TenantOptions("MultiTenant", "tenant_id", "default"))
            .GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Tenancy configuration"
            && check.Status == "Pass"
            && check.Detail.Contains("Multi-tenant mode", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_blocks_release_when_tenancy_mode_is_invalid()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            tenancy: new TenantOptions("Shared", "tenant_id", "default"))
            .GetStatus("TestService");

        Assert.Equal("Blocked", readiness.Status);
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Tenancy configuration"
            && check.Status == "Fail");
    }

    [Fact]
    public void GetStatus_marks_database_schema_management_manual_for_json_persistence()
    {
        var root = CreateRoot();
        var readiness = CreateService(root, aiApiKeyConfigured: true).GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database schema management"
            && check.Status == "Manual"
            && check.Detail.Contains("JSON persistence", StringComparison.Ordinal));
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database provider"
            && check.Status == "Manual"
            && check.Detail.Contains("JSON persistence", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_warns_when_database_uses_ensure_created_schema_mode()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            persistenceProvider: "Database",
            databaseProvider: "Sqlite",
            databaseConnectionString: "Data Source=knowledge-transfer.db")
            .GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database schema management"
            && check.Status == "Warning"
            && check.Detail.Contains("EnsureCreated", StringComparison.Ordinal));
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database provider"
            && check.Status == "Warning"
            && check.Detail.Contains("SQLite", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_passes_when_sql_server_database_provider_is_configured()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            persistenceProvider: "Database",
            databaseProvider: "SqlServer",
            databaseConnectionString: "Server=localhost;Database=AiKnowledgeTransfer;User Id=sa;Password=example;",
            databaseSchemaMode: "Migrations")
            .GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database provider"
            && check.Status == "Pass"
            && check.Detail.Contains("SQL Server", StringComparison.Ordinal));
    }

    [Fact]
    public void GetStatus_blocks_release_when_database_provider_is_unsupported()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            persistenceProvider: "Database",
            databaseProvider: "Postgres",
            databaseConnectionString: "Host=localhost;Database=AiKnowledgeTransfer",
            databaseSchemaMode: "Migrations")
            .GetStatus("TestService");

        Assert.Equal("Blocked", readiness.Status);
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database provider"
            && check.Status == "Fail");
    }

    [Fact]
    public void GetStatus_blocks_release_when_database_schema_mode_is_unsupported()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            persistenceProvider: "Database",
            databaseProvider: "Sqlite",
            databaseConnectionString: "Data Source=knowledge-transfer.db",
            databaseSchemaMode: "ManualSql")
            .GetStatus("TestService");

        Assert.Equal("Blocked", readiness.Status);
        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database schema management"
            && check.Status == "Fail");
    }

    [Fact]
    public void GetStatus_passes_when_database_uses_migrations_schema_mode()
    {
        var root = CreateRoot();
        var readiness = CreateService(
            root,
            aiApiKeyConfigured: true,
            persistenceProvider: "Database",
            databaseProvider: "Sqlite",
            databaseConnectionString: "Data Source=knowledge-transfer.db",
            databaseSchemaMode: "Migrations")
            .GetStatus("TestService");

        Assert.Contains(readiness.Checks, check =>
            check.Name == "Database schema management"
            && check.Status == "Pass"
            && check.Detail.Contains("managed EF migrations", StringComparison.Ordinal));
    }

    private static ReleaseReadinessService CreateService(
        string root,
        bool aiApiKeyConfigured,
        AuthenticationOptions? authentication = null,
        TenantOptions? tenancy = null,
        string persistenceProvider = "Json",
        string? databaseProvider = null,
        string? databaseConnectionString = null,
        string databaseSchemaMode = "EnsureCreated")
    {
        authentication ??= new AuthenticationOptions("Demo", null, null, "role");
        tenancy ??= TenantOptions.Default;
        var health = new OperationalHealthService(new OperationalHealthOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            persistenceProvider,
            databaseProvider,
            databaseConnectionString,
            databaseSchemaMode,
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
            tenancy.Mode,
            tenancy.TenantClaimType,
            tenancy.DefaultTenantId));
        var backups = new PersistenceBackupService(CreateBackupOptions(root));

        return new ReleaseReadinessService(health, backups, authentication);
    }

    private static PersistenceBackupOptions CreateBackupOptions(string root)
    {
        return new PersistenceBackupOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            Path.Combine(root, "backups"));
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-readiness-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
