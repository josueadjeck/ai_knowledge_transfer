namespace AiKnowledgeTransfer.Contracts.Security;

public sealed record RolePermissionReviewResponse(
    string Status,
    DateTimeOffset GeneratedAt,
    int RoleCount,
    int PermissionCount,
    int CriticalAssignmentCount,
    int NonAdminCriticalAssignmentCount,
    IReadOnlyCollection<RoleCriticalPermissionResponse> CriticalAssignments,
    IReadOnlyCollection<string> Notes);
