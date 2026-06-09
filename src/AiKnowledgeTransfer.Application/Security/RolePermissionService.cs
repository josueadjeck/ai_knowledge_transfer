namespace AiKnowledgeTransfer.Application.Security;

using AiKnowledgeTransfer.Contracts.Security;

public sealed class RolePermissionService
{
    private static readonly IReadOnlyDictionary<Permission, string> CriticalPermissions =
        new Dictionary<Permission, string>
        {
            [Permission.ManageUsers] = "Can manage users and operational administration.",
            [Permission.ApproveKnowledge] = "Can approve knowledge for onboarding use.",
            [Permission.RejectKnowledge] = "Can reject knowledge during expert review.",
            [Permission.ExportProject] = "Can export project knowledge outside the application.",
            [Permission.ViewTraceability] = "Can inspect source-to-knowledge traceability and audit context."
        };

    private static readonly IReadOnlyDictionary<UserRole, Permission[]> PermissionsByRole =
        new Dictionary<UserRole, Permission[]>
        {
            [UserRole.Admin] = Enum.GetValues<Permission>(),
            [UserRole.SeniorEngineer] =
            [
                Permission.ViewProject,
                Permission.CreateProject,
                Permission.UploadDocument,
                Permission.AnalyzeDocument,
                Permission.ExtractKnowledge,
                Permission.SubmitKnowledgeReview,
                Permission.ApproveKnowledge,
                Permission.RejectKnowledge,
                Permission.GenerateRoadmap,
                Permission.ViewTraceability,
                Permission.ExportProject
            ],
            [UserRole.Contributor] =
            [
                Permission.ViewProject,
                Permission.CreateProject,
                Permission.UploadDocument,
                Permission.AnalyzeDocument,
                Permission.ExtractKnowledge,
                Permission.SubmitKnowledgeReview,
                Permission.GenerateRoadmap,
                Permission.ViewTraceability,
                Permission.ExportProject
            ],
            [UserRole.Viewer] =
            [
                Permission.ViewProject,
                Permission.ViewTraceability,
                Permission.ExportProject
            ]
        };

    public bool HasPermission(UserRole role, Permission permission)
    {
        return PermissionsByRole.TryGetValue(role, out var permissions)
            && permissions.Contains(permission);
    }

    public RolePermissionResponse GetPermissions(UserRole role)
    {
        return new RolePermissionResponse(
            role,
            PermissionsByRole.TryGetValue(role, out var permissions) ? permissions : []);
    }

    public IReadOnlyCollection<RolePermissionResponse> GetMatrix()
    {
        return Enum.GetValues<UserRole>()
            .OrderBy(role => role)
            .Select(GetPermissions)
            .ToArray();
    }

    public RolePermissionReviewResponse GetReview()
    {
        var matrix = GetMatrix();
        var criticalAssignments = matrix
            .SelectMany(rolePermissions => rolePermissions.Permissions
                .Where(CriticalPermissions.ContainsKey)
                .Select(permission => new RoleCriticalPermissionResponse(
                    rolePermissions.Role,
                    permission,
                    CriticalPermissions[permission])))
            .OrderBy(assignment => assignment.Role)
            .ThenBy(assignment => assignment.Permission)
            .ToArray();
        var nonAdminCriticalAssignments = criticalAssignments
            .Count(assignment => assignment.Role != UserRole.Admin);
        var notes = BuildReviewNotes(nonAdminCriticalAssignments).ToArray();

        return new RolePermissionReviewResponse(
            nonAdminCriticalAssignments > 0 ? "NeedsReview" : "Ready",
            DateTimeOffset.UtcNow,
            matrix.Count,
            Enum.GetValues<Permission>().Length,
            criticalAssignments.Length,
            nonAdminCriticalAssignments,
            criticalAssignments,
            notes);
    }

    private static IEnumerable<string> BuildReviewNotes(int nonAdminCriticalAssignments)
    {
        yield return "Review non-admin roles with critical permissions before enterprise release.";
        yield return "Confirm role claim mapping in the identity provider matches the configured application roles.";

        if (nonAdminCriticalAssignments > 0)
        {
            yield return $"{nonAdminCriticalAssignments} critical permission assignments are held by non-admin roles.";
        }
    }
}
