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

    [Fact]
    public void HasPermission_denies_anonymous_access_in_oidc_mode()
    {
        var service = CreateService(new AuthenticationOptions(
            "Oidc",
            "https://login.example.test",
            "client-id",
            "roles"));

        Assert.False(service.HasPermission(new ClaimsPrincipal(new ClaimsIdentity()), Permission.ViewProject));
    }

    [Fact]
    public void HasPermission_allows_authenticated_oidc_role_permissions()
    {
        var options = new AuthenticationOptions(
            "Oidc",
            "https://login.example.test",
            "client-id",
            "roles");
        var service = CreateService(options);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("roles", "Admin")],
            authenticationType: "Bearer"));

        Assert.True(service.HasPermission(principal, Permission.ManageUsers));
    }

    private static PermissionAuthorizationService CreateService(AuthenticationOptions? options = null)
    {
        var permissions = new RolePermissionService();
        return new PermissionAuthorizationService(
            new UserIdentityService(permissions, options),
            options);
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "TestAuth"));
    }
}
