namespace AiKnowledgeTransfer.Application.Security;

using System.Security.Claims;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class UserIdentityService(
    RolePermissionService permissions,
    AuthenticationOptions? options = null)
{
    private readonly AuthenticationOptions _options = options ?? new AuthenticationOptions("Demo", null, null, ClaimTypes.Role);

    public CurrentUserResponse Resolve(ClaimsPrincipal principal, UserRole fallbackRole = UserRole.Viewer)
    {
        var isAuthenticated = principal.Identity?.IsAuthenticated == true;
        if (!isAuthenticated)
        {
            return CreateUser("anonymous", "Anonymous", fallbackRole, "Fallback", isAuthenticated: false);
        }

        var role = ResolveRole(principal.Claims) ?? fallbackRole;
        var userId = FirstClaimValue(principal, ClaimTypes.NameIdentifier)
            ?? FirstClaimValue(principal, "sub")
            ?? principal.Identity?.Name
            ?? "authenticated-user";
        var displayName = principal.Identity?.Name
            ?? FirstClaimValue(principal, "name")
            ?? userId;

        return CreateUser(userId, displayName, role, "Claims", isAuthenticated: true);
    }

    public CurrentUserResponse CreateDemoUser(UserRole role)
    {
        return CreateUser("demo-user", $"Demo {role}", role, "DemoRoleSelector", isAuthenticated: false);
    }

    private CurrentUserResponse CreateUser(
        string userId,
        string displayName,
        UserRole role,
        string source,
        bool isAuthenticated)
    {
        return new CurrentUserResponse(
            userId,
            displayName,
            role,
            permissions.GetPermissions(role).Permissions,
            source,
            isAuthenticated);
    }

    private UserRole? ResolveRole(IEnumerable<Claim> claims)
    {
        foreach (var claim in claims.Where(claim => IsRoleClaim(claim.Type)))
        {
            if (TryParseRole(claim.Value, out var role))
            {
                return role;
            }
        }

        return null;
    }

    private bool IsRoleClaim(string claimType)
    {
        return claimType.Equals(_options.RoleClaimType, StringComparison.OrdinalIgnoreCase)
            || claimType.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
            || claimType.Equals("role", StringComparison.OrdinalIgnoreCase)
            || claimType.Equals("roles", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseRole(string? value, out UserRole role)
    {
        role = UserRole.Viewer;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return Enum.TryParse(normalized, ignoreCase: true, out role);
    }

    private static string? FirstClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
    }
}
