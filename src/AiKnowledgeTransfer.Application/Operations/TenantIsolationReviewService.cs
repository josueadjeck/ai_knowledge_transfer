namespace AiKnowledgeTransfer.Application.Operations;

using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Operations;

public sealed class TenantIsolationReviewService(TenantOptions options)
{
    public TenantIsolationReviewResponse GetReview()
    {
        var areas = BuildAreas().ToArray();
        var status = !options.IsConfigured
            ? "Blocked"
            : options.IsMultiTenant
                ? "NeedsReview"
                : options.IsDedicatedDeploymentBoundary
                    ? "Pass"
                    : "SingleTenant";
        var manualAreaCount = areas.Count(area => area.Status.Equals("Manual", StringComparison.OrdinalIgnoreCase));

        return new TenantIsolationReviewResponse(
            status,
            DateTimeOffset.UtcNow,
            options.Mode,
            options.TenantClaimType,
            options.DefaultTenantId,
            areas.Length,
            manualAreaCount,
            areas,
            BuildNotes().ToArray());
    }

    private IEnumerable<TenantIsolationAreaResponse> BuildAreas()
    {
        if (!options.IsConfigured)
        {
            yield return new TenantIsolationAreaResponse(
                "Configuration",
                "Fail",
                "Tenancy mode, tenant claim and default tenant id must be configured.");
            yield break;
        }

        yield return new TenantIsolationAreaResponse(
            "Configuration",
            "Pass",
            $"Mode {options.Mode}, tenant claim {options.TenantClaimType}, default tenant {options.DefaultTenantId}, boundary mode {options.BoundaryMode}.");

        foreach (var area in new[]
        {
            "Persistence records",
            "Upload file storage",
            "Audit events",
            "Backups and restore scope",
            "API authorization",
            "UI filtering",
            "Exports"
        })
        {
            if (options.IsSingleTenant && options.IsDedicatedDeploymentBoundary)
            {
                yield return new TenantIsolationAreaResponse(
                    area,
                    "Pass",
                    $"Dedicated deployment boundary is configured and owned by {options.BoundaryOwner}; this deployment serves one tenant or customer boundary.");
                continue;
            }

            yield return new TenantIsolationAreaResponse(
                area,
                options.IsMultiTenant ? "Manual" : "Planned",
                options.IsMultiTenant
                    ? "Confirm tenant boundary design and implementation before serving multiple tenants."
                    : "Single-tenant mode is active; this area is planned for future multi-tenant implementation.");
        }
    }

    private IEnumerable<string> BuildNotes()
    {
        if (!options.IsConfigured)
        {
            yield return "Tenancy configuration is invalid; release readiness should block deployment.";
            yield break;
        }

        if (options.IsMultiTenant)
        {
            yield return "MultiTenant mode requires a manual isolation review because full tenant isolation is not implemented end to end.";
            yield return "Do not host multiple customers until persistence, storage, audit, backup, API, UI and export isolation are verified.";
            yield break;
        }

        if (options.IsDedicatedDeploymentBoundary)
        {
            yield return "SingleTenant mode is isolated by a dedicated deployment boundary.";
            yield return "Use a separate deployment, storage root, database, backup scope and release approval per customer or tenant boundary.";
            yield break;
        }

        yield return "SingleTenant mode is the supported MVP operating mode.";
        yield return "Set AKT_TENANT_BOUNDARY_MODE=DedicatedDeployment and AKT_TENANT_BOUNDARY_OWNER before claiming release-ready tenant isolation.";
    }
}
