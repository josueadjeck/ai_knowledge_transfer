namespace AiKnowledgeTransfer.Application.Abstractions;

public sealed class NullAuditLog : IAuditLog
{
    public static NullAuditLog Instance { get; } = new();

    private NullAuditLog()
    {
    }

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuditEvent>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<AuditEvent>>([]);
    }
}
