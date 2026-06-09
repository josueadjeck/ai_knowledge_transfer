namespace AiKnowledgeTransfer.UnitTests;

using System.Security.Claims;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class PermissionAuthorizationServiceTests
{
    [Fact]
    public void HasPermission_allows_claim_role_permissions()
    {
        var service = CreateService();
        var principal = CreatePrincipal("SeniorEngineer");

        Assert.True(service.HasPermission(principal, Permission.ApproveKnowledge));
        Assert.False(service.HasPermission(principal, Permission.ManageUsers));
    }

    [Fact]
    public void HasPermission_denies_anonymous_mutating_permissions()
    {
        var service = CreateService();

        Assert.False(service.HasPermission(new ClaimsPrincipal(new ClaimsIdentity()), Permission.CreateProject));
        Assert.True(service.HasPermission(new ClaimsPrincipal(new ClaimsIdentity()), Permission.ViewProject));
    }

    private static PermissionAuthorizationService CreateService()
    {
        return new PermissionAuthorizationService(new UserIdentityService(new RolePermissionService()));
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "TestAuth"));
    }
}
