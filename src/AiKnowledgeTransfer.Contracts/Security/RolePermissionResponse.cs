namespace AiKnowledgeTransfer.Contracts.Security;

public sealed record RolePermissionResponse(
    UserRole Role,
    IReadOnlyCollection<Permission> Permissions);
