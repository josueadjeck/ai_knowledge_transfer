namespace AiKnowledgeTransfer.Contracts.Security;

public sealed record CurrentUserResponse(
    string UserId,
    string DisplayName,
    UserRole Role,
    IReadOnlyCollection<Permission> Permissions,
    string Source,
    bool IsAuthenticated);
