namespace AiKnowledgeTransfer.Application.Security;

using System.Security.Claims;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class PermissionAuthorizationService(
    UserIdentityService identity,
    AuthenticationOptions? options = null)
{
    private readonly AuthenticationOptions _options = options ?? new AuthenticationOptions("Demo", null, null, "role");

    public bool HasPermission(ClaimsPrincipal principal, Permission permission)
    {
        if (_options.IsOidc && principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return identity.Resolve(principal).Permissions.Contains(permission);
    }
}
