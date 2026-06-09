namespace AiKnowledgeTransfer.Application.Security;

public sealed record AuthenticationOptions(
    string Mode,
    string? Authority,
    string? ClientId,
    string RoleClaimType)
{
    public static AuthenticationOptions FromEnvironment()
    {
        var mode = Environment.GetEnvironmentVariable("AKT_AUTH_MODE");
        if (string.IsNullOrWhiteSpace(mode))
        {
            mode = "Demo";
        }

        var roleClaimType = Environment.GetEnvironmentVariable("AKT_AUTH_ROLE_CLAIM");
        if (string.IsNullOrWhiteSpace(roleClaimType))
        {
            roleClaimType = "role";
        }

        return new AuthenticationOptions(
            mode,
            Environment.GetEnvironmentVariable("AKT_AUTH_AUTHORITY"),
            Environment.GetEnvironmentVariable("AKT_AUTH_CLIENT_ID"),
            roleClaimType);
    }

    public bool IsOidc => Mode.Equals("Oidc", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured => !IsOidc
        || (!string.IsNullOrWhiteSpace(Authority)
            && !string.IsNullOrWhiteSpace(ClientId));
}
