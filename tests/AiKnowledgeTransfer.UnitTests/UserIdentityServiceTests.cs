namespace AiKnowledgeTransfer.UnitTests;

using System.Security.Claims;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class UserIdentityServiceTests
{
    [Fact]
    public void Resolve_maps_authenticated_role_claim_to_permissions()
    {
        var service = new UserIdentityService(new RolePermissionService());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Name, "Ada Engineer"),
                new Claim(ClaimTypes.Role, "Senior Engineer")
            ],
            authenticationType: "TestAuth"));

        var user = service.Resolve(principal);

        Assert.True(user.IsAuthenticated);
        Assert.Equal("user-123", user.UserId);
        Assert.Equal("Ada Engineer", user.DisplayName);
        Assert.Equal(UserRole.SeniorEngineer, user.Role);
        Assert.Equal("Claims", user.Source);
        Assert.Contains(Permission.ApproveKnowledge, user.Permissions);
        Assert.DoesNotContain(Permission.ManageUsers, user.Permissions);
    }

    [Fact]
    public void Resolve_uses_viewer_fallback_for_anonymous_users()
    {
        var service = new UserIdentityService(new RolePermissionService());

        var user = service.Resolve(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(user.IsAuthenticated);
        Assert.Equal(UserRole.Viewer, user.Role);
        Assert.Equal("Fallback", user.Source);
        Assert.Contains(Permission.ViewProject, user.Permissions);
        Assert.DoesNotContain(Permission.UploadDocument, user.Permissions);
    }

    [Fact]
    public void CreateDemoUser_uses_selected_mvp_role()
    {
        var service = new UserIdentityService(new RolePermissionService());

        var user = service.CreateDemoUser(UserRole.Admin);

        Assert.False(user.IsAuthenticated);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal("DemoRoleSelector", user.Source);
        Assert.Contains(Permission.ManageUsers, user.Permissions);
    }

    [Fact]
    public void Resolve_uses_configured_role_claim_type()
    {
        var service = new UserIdentityService(
            new RolePermissionService(),
            new AuthenticationOptions("Oidc", "https://login.example.test", "client-id", "groups"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("groups", "Admin")],
            authenticationType: "TestAuth"));

        var user = service.Resolve(principal);

        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Contains(Permission.ManageUsers, user.Permissions);
    }
}
