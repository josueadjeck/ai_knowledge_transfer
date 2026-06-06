namespace AiKnowledgeTransfer.Application.Audit;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Audit;

public sealed class AuditLogService(IAuditLog auditLog)
{
    public async Task<IReadOnlyCollection<AuditEventResponse>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var events = await auditLog.ListAsync(projectId, cancellationToken);
        return events
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Select(ToResponse)
            .ToArray();
    }

    private static AuditEventResponse ToResponse(AuditEvent auditEvent)
    {
        return new AuditEventResponse(
            auditEvent.Id,
            auditEvent.ProjectId,
            auditEvent.Action,
            auditEvent.Actor,
            auditEvent.TargetType,
            auditEvent.TargetId,
            auditEvent.Summary,
            auditEvent.OccurredAt);
    }
}
