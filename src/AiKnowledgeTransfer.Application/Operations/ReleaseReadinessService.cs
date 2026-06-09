namespace AiKnowledgeTransfer.Application.Operations;

using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Diagnostics;
using AiKnowledgeTransfer.Contracts.Operations;
using Microsoft.Extensions.Logging;

public sealed class ReleaseReadinessService(
    OperationalHealthService health,
    PersistenceBackupService backups,
    AuthenticationOptions authentication,
    ILogger<ReleaseReadinessService>? logger = null)
{
    public ReleaseReadinessResponse GetStatus(string serviceName)
    {
        var healthStatus = health.GetStatus(serviceName);
        var backupList = backups.List();
        var checks = new List<ReleaseReadinessCheckResponse>
        {
            BuildHealthCheck(healthStatus),
            BuildPersistenceCheck(healthStatus),
            BuildDatabaseSchemaCheck(healthStatus),
            BuildAuthenticationCheck(authentication),
            BuildTenancyCheck(healthStatus),
            BuildBackupCheck(backupList),
            BuildAiProviderCheck(healthStatus),
            new(
                "CI security gates",
                "Manual",
                Required: true,
                "Confirm the latest GitHub Actions CI run passed before deployment.",
                ".github/workflows/ci.yml runs restore, build, publish, container build, tests, vulnerability check and basic secret scan."),
            new(
                "Security inventory",
                "Manual",
                Required: true,
                "Confirm the CI security-inventory artifact was generated and reviewed for the release.",
                "Expected artifact: security-inventory with dotnet-package-inventory.json and vulnerable-packages.json."),
            new(
                "Container image inventory",
                "Manual",
                Required: true,
                "Confirm the CI security-inventory artifact includes metadata for the API and Web container images.",
                "Expected artifact entry: container-images.json for ai-knowledge-transfer-api:ci and ai-knowledge-transfer-web:ci."),
            new(
                "Security and data protection review",
                "Manual",
                Required: true,
                "Confirm the security concept and data protection concept were reviewed for this release.",
                "Review docs/security/security-concept.md and docs/security/data-protection-concept.md."),
            new(
                "Release approval",
                "Manual",
                Required: true,
                "Confirm a human release owner approved the deployment.",
                "Use repository branch protection or a release checklist entry.")
        };

        var status = checks.Any(check => check.Required && check.Status == "Fail")
            ? "Blocked"
            : checks.Any(check => check.Status is "Warning" or "Manual")
                ? "NeedsReview"
                : "Ready";

        logger?.LogInformation(
            "Release readiness checked for {ServiceName}: {Status} with {CheckCount} checks.",
            serviceName,
            status,
            checks.Count);

        return new ReleaseReadinessResponse(status, serviceName, DateTimeOffset.UtcNow, checks);
    }

    private static ReleaseReadinessCheckResponse BuildHealthCheck(HealthResponse healthStatus)
    {
        return new ReleaseReadinessCheckResponse(
            "Runtime health",
            healthStatus.Status == "ok" ? "Pass" : "Fail",
            Required: true,
            healthStatus.Status == "ok"
                ? "All runtime health components are available."
                : "One or more runtime health components are degraded.",
            $"Health status: {healthStatus.Status}");
    }

    private static ReleaseReadinessCheckResponse BuildPersistenceCheck(HealthResponse healthStatus)
    {
        var persistence = healthStatus.Components.FirstOrDefault(component => component.Name == "persistence");
        var status = persistence?.Status == "error" ? "Fail" : "Pass";
        var provider = persistence?.Metadata.TryGetValue("provider", out var value) == true ? value : "unknown";

        return new ReleaseReadinessCheckResponse(
            "Persistence configuration",
            status,
            Required: true,
            status == "Pass"
                ? "Persistence configuration is usable."
                : "Persistence configuration is incomplete.",
            $"Provider: {provider}");
    }

    private static ReleaseReadinessCheckResponse BuildDatabaseSchemaCheck(HealthResponse healthStatus)
    {
        var persistence = healthStatus.Components.FirstOrDefault(component => component.Name == "persistence");
        var provider = persistence?.Metadata.TryGetValue("provider", out var providerValue) == true
            ? providerValue
            : "unknown";
        var schemaMode = persistence?.Metadata.TryGetValue("databaseSchemaMode", out var schemaModeValue) == true
            ? schemaModeValue
            : "unknown";

        if (!provider.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Database schema management",
                "Manual",
                Required: false,
                "JSON persistence is active; database schema management does not apply to this deployment.",
                $"Persistence: {provider}");
        }

        if (schemaMode.Equals("EnsureCreated", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Database schema management",
                "Warning",
                Required: false,
                "Database mode uses EF EnsureCreated; add managed EF migrations before production database rollout.",
                $"Schema mode: {schemaMode}");
        }

        if (schemaMode.Equals("Migrations", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Database schema management",
                "Pass",
                Required: true,
                "Database mode uses managed EF migrations.",
                $"Schema mode: {schemaMode}");
        }

        return new ReleaseReadinessCheckResponse(
            "Database schema management",
            "Fail",
            Required: true,
            "Unsupported database schema mode.",
            $"Schema mode: {schemaMode}");
    }

    private static ReleaseReadinessCheckResponse BuildBackupCheck(IReadOnlyCollection<BackupResponse> backupList)
    {
        var latestBackup = backupList.OrderByDescending(backup => backup.CreatedAt).FirstOrDefault();
        if (latestBackup is null)
        {
            return new ReleaseReadinessCheckResponse(
                "Backup available",
                "Fail",
                Required: true,
                "Create and preview a backup before deployment.",
                "No backup file found.");
        }

        return new ReleaseReadinessCheckResponse(
            "Backup available",
            "Pass",
            Required: true,
            "At least one local backup exists.",
            $"{latestBackup.FileName}, {latestBackup.CreatedAt:u}");
    }

    private static ReleaseReadinessCheckResponse BuildAuthenticationCheck(AuthenticationOptions authentication)
    {
        if (!authentication.IsConfigured)
        {
            return new ReleaseReadinessCheckResponse(
                "Authentication configuration",
                "Fail",
                Required: true,
                "OIDC mode requires authority and client id before release.",
                $"Mode: {authentication.Mode}");
        }

        if (!authentication.IsOidc)
        {
            return new ReleaseReadinessCheckResponse(
                "Authentication configuration",
                "Warning",
                Required: true,
                "Demo authentication mode is active; use OIDC for enterprise deployment.",
                $"Mode: {authentication.Mode}, role claim: {authentication.RoleClaimType}");
        }

        return new ReleaseReadinessCheckResponse(
            "Authentication configuration",
            "Pass",
            Required: true,
            "OIDC authentication settings are configured.",
            $"Mode: {authentication.Mode}, role claim: {authentication.RoleClaimType}");
    }

    private static ReleaseReadinessCheckResponse BuildTenancyCheck(HealthResponse healthStatus)
    {
        var tenancy = healthStatus.Components.FirstOrDefault(component => component.Name == "tenancy");
        if (tenancy is null)
        {
            return new ReleaseReadinessCheckResponse(
                "Tenancy configuration",
                "Fail",
                Required: true,
                "Tenancy health component is missing.",
                "Expected health component: tenancy.");
        }

        var mode = tenancy.Metadata.TryGetValue("mode", out var configuredMode)
            ? configuredMode
            : "unknown";

        if (tenancy.Status == "error")
        {
            return new ReleaseReadinessCheckResponse(
                "Tenancy configuration",
                "Fail",
                Required: true,
                "Tenancy configuration is invalid.",
                $"Mode: {mode}");
        }

        if (mode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Tenancy configuration",
                "Pass",
                Required: false,
                "Multi-tenant mode is configured.",
                $"Mode: {mode}");
        }

        return new ReleaseReadinessCheckResponse(
            "Tenancy configuration",
            "Warning",
            Required: false,
            "Single-tenant MVP mode is active; multi-tenant isolation must be designed before serving multiple customers.",
            $"Mode: {mode}");
    }

    private static ReleaseReadinessCheckResponse BuildAiProviderCheck(HealthResponse healthStatus)
    {
        var aiProvider = healthStatus.Components.FirstOrDefault(component => component.Name == "aiProvider");
        var status = aiProvider?.Status == "ok" ? "Pass" : "Warning";

        return new ReleaseReadinessCheckResponse(
            "AI provider",
            status,
            Required: false,
            status == "Pass"
                ? "Configured AI provider is available for extraction."
                : "AI provider is not configured; release can continue only if heuristic fallback is acceptable.",
            aiProvider?.Detail ?? "AI provider health component missing.");
    }
}
