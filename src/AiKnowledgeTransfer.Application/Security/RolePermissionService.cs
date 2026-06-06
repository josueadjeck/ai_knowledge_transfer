namespace AiKnowledgeTransfer.Application.Security;

using AiKnowledgeTransfer.Contracts.Security;

public sealed class RolePermissionService
{
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
}
