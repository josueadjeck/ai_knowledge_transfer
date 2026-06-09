namespace AiKnowledgeTransfer.Application.Operations;

using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Diagnostics;
using AiKnowledgeTransfer.Contracts.Operations;
using AiKnowledgeTransfer.Contracts.Security;
using Microsoft.Extensions.Logging;

public sealed class ReleaseReadinessService(
    OperationalHealthService health,
    PersistenceBackupService backups,
    AuthenticationOptions authentication,
    SecretManagementOptions? secretManagement = null,
    DeploymentOptions? deployment = null,
    RolePermissionService? rolePermissions = null,
    TenantIsolationReviewService? tenantIsolationReview = null,
    ILogger<ReleaseReadinessService>? logger = null)
{
    private readonly RolePermissionService _rolePermissions = rolePermissions ?? new RolePermissionService();
    private readonly TenantIsolationReviewService? _tenantIsolationReview = tenantIsolationReview;
    private readonly SecretManagementOptions _secretManagement = secretManagement ?? SecretManagementOptions.Default;
    private readonly DeploymentOptions _deployment = deployment ?? DeploymentOptions.Default;

    public ReleaseReadinessResponse GetStatus(string serviceName)
    {
        var healthStatus = health.GetStatus(serviceName);
        var backupList = backups.List();
        var checks = new List<ReleaseReadinessCheckResponse>
        {
            BuildHealthCheck(healthStatus),
            BuildPersistenceCheck(healthStatus),
            BuildDatabaseSchemaCheck(healthStatus),
            BuildDatabaseProviderCheck(healthStatus),
            BuildAuthenticationCheck(authentication),
            BuildRolePermissionReviewCheck(_rolePermissions.GetReview()),
            BuildTenancyCheck(healthStatus),
            BuildTenantIsolationReviewCheck(_tenantIsolationReview?.GetReview(), healthStatus),
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
                "Expected artifact: security-inventory with build-artifact-inventory.json, dotnet-package-inventory.json, sbom.spdx.json, cyclonedx-build-sbom.json, vulnerable-packages.json and static-source-scan.json."),
            new(
                "Container image inventory",
                "Manual",
                Required: true,
                "Confirm the CI security-inventory artifact includes metadata, provenance and signing policy for the API and Web container images.",
                "Expected artifact entries: container-policy-scan.json, container-vulnerability-scan-api.json, container-vulnerability-scan-web.json, container-images.json, container-provenance.json and container-signing-policy.json for ai-knowledge-transfer-api:ci and ai-knowledge-transfer-web:ci."),
            new(
                "Security and data protection review",
                "Manual",
                Required: true,
                "Confirm the security concept and data protection concept were reviewed for this release.",
                "Review docs/security/security-concept.md and docs/security/data-protection-concept.md."),
            BuildSecretManagementCheck(_secretManagement),
            BuildDeploymentStrategyCheck(_deployment),
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

    private static ReleaseReadinessCheckResponse BuildDatabaseProviderCheck(HealthResponse healthStatus)
    {
        var persistence = healthStatus.Components.FirstOrDefault(component => component.Name == "persistence");
        var provider = persistence?.Metadata.TryGetValue("provider", out var providerValue) == true
            ? providerValue
            : "unknown";
        var databaseProvider = persistence?.Metadata.TryGetValue("databaseProvider", out var databaseProviderValue) == true
            ? databaseProviderValue
            : string.Empty;

        if (!provider.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Database provider",
                "Manual",
                Required: false,
                "JSON persistence is active; database provider review does not apply to this deployment.",
                $"Persistence: {provider}");
        }

        if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            || databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            || databaseProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
            || databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Database provider",
                "Pass",
                Required: true,
                "A production-capable relational database provider is configured.",
                $"Database provider: {databaseProvider}");
        }

        if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseReadinessCheckResponse(
                "Database provider",
                "Warning",
                Required: false,
                "SQLite is configured; use it for local pilots, not enterprise production.",
                $"Database provider: {databaseProvider}");
        }

        return new ReleaseReadinessCheckResponse(
            "Database provider",
            "Fail",
            Required: true,
            "Unsupported database provider for release.",
            $"Database provider: {databaseProvider}");
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

    private static ReleaseReadinessCheckResponse BuildRolePermissionReviewCheck(RolePermissionReviewResponse review)
    {
        return new ReleaseReadinessCheckResponse(
            "Role permission review",
            "Manual",
            Required: true,
            "Confirm critical permissions and identity-provider role mappings are reviewed before release.",
            $"Status: {review.Status}, non-admin critical assignments: {review.NonAdminCriticalAssignmentCount}, endpoint: /api/security/role-review");
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
                "Manual",
                Required: false,
                "Multi-tenant mode is configured; complete isolation still requires manual release review.",
                $"Mode: {mode}");
        }

        return new ReleaseReadinessCheckResponse(
            "Tenancy configuration",
            "Warning",
            Required: false,
            "Single-tenant MVP mode is active; multi-tenant isolation must be designed before serving multiple customers.",
            $"Mode: {mode}");
    }

    private static ReleaseReadinessCheckResponse BuildTenantIsolationReviewCheck(
        TenantIsolationReviewResponse? review,
        HealthResponse healthStatus)
    {
        var tenancy = healthStatus.Components.FirstOrDefault(component => component.Name == "tenancy");
        var mode = tenancy?.Metadata.TryGetValue("mode", out var configuredMode) == true
            ? configuredMode
            : "unknown";

        if (review is null)
        {
            return new ReleaseReadinessCheckResponse(
                "Tenant isolation review",
                "Manual",
                Required: true,
                "Confirm tenant isolation assumptions before release.",
                $"Mode: {mode}, endpoint: /api/operations/tenant-isolation-review");
        }

        return new ReleaseReadinessCheckResponse(
            "Tenant isolation review",
            review.Status == "Blocked"
                ? "Fail"
                : review.Status == "Pass"
                    ? "Pass"
                    : "Manual",
            Required: true,
            review.Status == "SingleTenant"
                ? "Single-tenant MVP mode is accepted only for one customer or one isolated deployment."
                : review.Status == "Pass"
                    ? "Dedicated deployment tenant boundary is configured."
                : "Review tenant isolation areas before release.",
            $"Status: {review.Status}, manual areas: {review.ManualAreaCount}, endpoint: /api/operations/tenant-isolation-review");
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

    private static ReleaseReadinessCheckResponse BuildSecretManagementCheck(SecretManagementOptions options)
    {
        if (options.IsSecretStore && options.IsConfigured)
        {
            return new ReleaseReadinessCheckResponse(
                "Secret management review",
                "Pass",
                Required: true,
                "Runtime secrets are configured for secret-store injection.",
                $"Mode: {options.Mode}, provider configured: True, rotation owner configured: True, docs: docs/security/secret-management.md");
        }

        if (options.IsSecretStore)
        {
            return new ReleaseReadinessCheckResponse(
                "Secret management review",
                "Fail",
                Required: true,
                "Secret-store mode is selected but provider or rotation owner configuration is incomplete.",
                $"Mode: {options.Mode}, provider configured: {(!string.IsNullOrWhiteSpace(options.Provider)).ToString()}, rotation owner configured: {(!string.IsNullOrWhiteSpace(options.RotationOwner)).ToString()}");
        }

        return new ReleaseReadinessCheckResponse(
            "Secret management review",
            "Manual",
            Required: true,
            "Confirm runtime secrets are injected through an approved environment or secret store and rotation is documented.",
            "Review docs/security/secret-management.md; required sensitive settings include OPENAI_API_KEY and AKT_DB_CONNECTION_STRING when used.");
    }

    private static ReleaseReadinessCheckResponse BuildDeploymentStrategyCheck(DeploymentOptions options)
    {
        if (options.IsBlueGreen && options.IsConfigured)
        {
            return new ReleaseReadinessCheckResponse(
                "Deployment strategy",
                "Pass",
                Required: true,
                "Blue/green deployment strategy is configured for this release.",
                $"Strategy: {options.Strategy}, approval owner configured: True, docs: docs/operations/deployment.md");
        }

        if (options.IsBlueGreen)
        {
            return new ReleaseReadinessCheckResponse(
                "Deployment strategy",
                "Fail",
                Required: true,
                "Blue/green deployment strategy is selected but approval owner metadata is missing.",
                $"Strategy: {options.Strategy}, approval owner configured: {(!string.IsNullOrWhiteSpace(options.ApprovalOwner)).ToString()}");
        }

        if (options.IsSingleSlot)
        {
            return new ReleaseReadinessCheckResponse(
                "Deployment strategy",
                "Manual",
                Required: false,
                "Single-slot deployment is active; confirm downtime and rollback risk are accepted for this release.",
                $"Strategy: {options.Strategy}");
        }

        return new ReleaseReadinessCheckResponse(
            "Deployment strategy",
            "Fail",
            Required: true,
            "Unsupported deployment strategy.",
            $"Strategy: {options.Strategy}");
    }
}
