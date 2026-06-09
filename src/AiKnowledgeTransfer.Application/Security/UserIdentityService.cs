namespace AiKnowledgeTransfer.Application.Security;

using System.Security.Claims;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class UserIdentityService(RolePermissionService permissions)
{
    public CurrentUserResponse Resolve(ClaimsPrincipal principal, UserRole fallbackRole = UserRole.Viewer)
    {
        var isAuthenticated = principal.Identity?.IsAuthenticated == true;
        if (!isAuthenticated)
        {
            return CreateUser("anonymous", "Anonymous", fallbackRole, "Fallback", isAuthenticated: false);
        }

        var role = ResolveRole(principal.Claims.Select(claim => claim.Value)) ?? fallbackRole;
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

    private static UserRole? ResolveRole(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            if (TryParseRole(value, out var role))
            {
                return role;
            }
        }

        return null;
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
