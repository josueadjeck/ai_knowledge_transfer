namespace AiKnowledgeTransfer.Application.Security;

public sealed record TenantOptions(
    string Mode,
    string TenantClaimType,
    string DefaultTenantId,
    string BoundaryMode = "Unspecified",
    string? BoundaryOwner = null)
{
    public const string ModeEnvironmentVariable = "AKT_TENANCY_MODE";
    public const string TenantClaimEnvironmentVariable = "AKT_TENANT_CLAIM";
    public const string DefaultTenantEnvironmentVariable = "AKT_DEFAULT_TENANT_ID";
    public const string BoundaryModeEnvironmentVariable = "AKT_TENANT_BOUNDARY_MODE";
    public const string BoundaryOwnerEnvironmentVariable = "AKT_TENANT_BOUNDARY_OWNER";

    public static TenantOptions Default { get; } = new("SingleTenant", "tenant_id", "default");

    public static TenantOptions FromEnvironment()
    {
        return new TenantOptions(
            ReadEnvironmentValue(ModeEnvironmentVariable, Default.Mode),
            ReadEnvironmentValue(TenantClaimEnvironmentVariable, Default.TenantClaimType),
            ReadEnvironmentValue(DefaultTenantEnvironmentVariable, Default.DefaultTenantId),
            ReadEnvironmentValue(BoundaryModeEnvironmentVariable, Default.BoundaryMode),
            Environment.GetEnvironmentVariable(BoundaryOwnerEnvironmentVariable));
    }

    public bool IsMultiTenant => Mode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);

    public bool IsSingleTenant => Mode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured => (IsSingleTenant || IsMultiTenant)
        && !string.IsNullOrWhiteSpace(TenantClaimType)
        && !string.IsNullOrWhiteSpace(DefaultTenantId);

    public bool IsDedicatedDeploymentBoundary => BoundaryMode.Equals("DedicatedDeployment", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(BoundaryOwner);

    private static string ReadEnvironmentValue(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
