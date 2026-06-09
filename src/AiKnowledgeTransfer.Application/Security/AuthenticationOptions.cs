namespace AiKnowledgeTransfer.Application.Security;

public sealed record AuthenticationOptions(
    string Mode,
    string? Authority,
    string? ClientId,
    string RoleClaimType)
{
    public bool IsOidc => Mode.Equals("Oidc", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured => !IsOidc
        || (!string.IsNullOrWhiteSpace(Authority)
            && !string.IsNullOrWhiteSpace(ClientId));
}
