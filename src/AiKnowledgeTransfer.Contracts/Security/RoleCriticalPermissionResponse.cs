namespace AiKnowledgeTransfer.Contracts.Security;

public sealed record RoleCriticalPermissionResponse(
    UserRole Role,
    Permission Permission,
    string Reason);
