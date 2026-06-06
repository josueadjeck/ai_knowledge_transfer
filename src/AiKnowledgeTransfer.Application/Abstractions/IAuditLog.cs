namespace AiKnowledgeTransfer.Application.Abstractions;

public interface IAuditLog
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditEvent>> ListAsync(Guid? projectId, CancellationToken cancellationToken);
}
