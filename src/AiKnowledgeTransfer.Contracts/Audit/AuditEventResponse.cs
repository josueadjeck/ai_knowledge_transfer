namespace AiKnowledgeTransfer.Contracts.Audit;

public sealed record AuditEventResponse(
    Guid Id,
    Guid? ProjectId,
    string Action,
    string Actor,
    string TargetType,
    Guid? TargetId,
    string Summary,
    DateTimeOffset OccurredAt);
