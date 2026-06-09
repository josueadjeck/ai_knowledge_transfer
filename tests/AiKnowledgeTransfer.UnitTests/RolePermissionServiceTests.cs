namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Security;

public sealed class RolePermissionServiceTests
{
    [Fact]
    public void HasPermission_allows_admin_to_manage_users()
    {
        var service = new RolePermissionService();

        Assert.True(service.HasPermission(UserRole.Admin, Permission.ManageUsers));
    }

    [Fact]
    public void HasPermission_allows_contributor_to_submit_but_not_approve_reviews()
    {
        var service = new RolePermissionService();

        Assert.True(service.HasPermission(UserRole.Contributor, Permission.SubmitKnowledgeReview));
        Assert.False(service.HasPermission(UserRole.Contributor, Permission.ApproveKnowledge));
    }

    [Fact]
    public void HasPermission_limits_viewer_to_read_and_export_actions()
    {
        var service = new RolePermissionService();

        Assert.True(service.HasPermission(UserRole.Viewer, Permission.ViewProject));
        Assert.True(service.HasPermission(UserRole.Viewer, Permission.ExportProject));
        Assert.False(service.HasPermission(UserRole.Viewer, Permission.UploadDocument));
    }

    [Fact]
    public void GetReview_reports_non_admin_critical_assignments()
    {
        var service = new RolePermissionService();

        var review = service.GetReview();

        Assert.Equal("NeedsReview", review.Status);
        Assert.Equal(4, review.RoleCount);
        Assert.True(review.CriticalAssignmentCount > 0);
        Assert.True(review.NonAdminCriticalAssignmentCount > 0);
        Assert.Contains(review.CriticalAssignments, assignment =>
            assignment.Role == UserRole.SeniorEngineer
            && assignment.Permission == Permission.ApproveKnowledge);
        Assert.Contains(review.Notes, note =>
            note.Contains("identity provider", StringComparison.OrdinalIgnoreCase));
    }
}
