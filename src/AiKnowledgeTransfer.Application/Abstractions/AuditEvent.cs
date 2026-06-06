namespace AiKnowledgeTransfer.Application.Abstractions;

public sealed record AuditEvent(
    Guid Id,
    Guid? ProjectId,
    string Action,
    string Actor,
    string TargetType,
    Guid? TargetId,
    string Summary,
    DateTimeOffset OccurredAt)
{
    public static AuditEvent Create(
        Guid? projectId,
        string action,
        string actor,
        string targetType,
        Guid? targetId,
        string summary)
    {
        return new AuditEvent(
            Guid.NewGuid(),
            projectId,
            action,
            string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
            targetType.Trim(),
            targetId,
            summary.Trim(),
            DateTimeOffset.UtcNow);
    }
}
