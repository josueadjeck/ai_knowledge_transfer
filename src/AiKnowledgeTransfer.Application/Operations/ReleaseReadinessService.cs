namespace AiKnowledgeTransfer.Application.Operations;

using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Diagnostics;
using AiKnowledgeTransfer.Contracts.Operations;

public sealed class ReleaseReadinessService(
    OperationalHealthService health,
    PersistenceBackupService backups,
    AuthenticationOptions authentication)
{
    public ReleaseReadinessResponse GetStatus(string serviceName)
    {
        var healthStatus = health.GetStatus(serviceName);
        var backupList = backups.List();
        var checks = new List<ReleaseReadinessCheckResponse>
        {
            BuildHealthCheck(healthStatus),
            BuildPersistenceCheck(healthStatus),
            BuildAuthenticationCheck(authentication),
            BuildBackupCheck(backupList),
            BuildAiProviderCheck(healthStatus),
            new(
                "CI security gates",
                "Manual",
                Required: true,
                "Confirm the latest GitHub Actions CI run passed before deployment.",
                ".github/workflows/ci.yml runs restore, build, tests, vulnerability check and basic secret scan."),
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
        if (authentication.IsConfigured)
        {
            return new ReleaseReadinessCheckResponse(
                "Authentication configuration",
                "Pass",
                Required: true,
                authentication.IsOidc
                    ? "OIDC authentication settings are configured."
                    : "Demo authentication mode is active.",
                $"Mode: {authentication.Mode}, role claim: {authentication.RoleClaimType}");
        }

        return new ReleaseReadinessCheckResponse(
            "Authentication configuration",
            "Fail",
            Required: true,
            "OIDC mode requires authority and client id before release.",
            $"Mode: {authentication.Mode}");
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
