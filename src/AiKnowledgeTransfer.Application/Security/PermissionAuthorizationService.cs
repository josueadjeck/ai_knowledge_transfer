namespace AiKnowledgeTransfer.Application.Security;

using System.Security.Claims;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class PermissionAuthorizationService(UserIdentityService identity)
{
    public bool HasPermission(ClaimsPrincipal principal, Permission permission)
    {
        return identity.Resolve(principal).Permissions.Contains(permission);
    }
}
