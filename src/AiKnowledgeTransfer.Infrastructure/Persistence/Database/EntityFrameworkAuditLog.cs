using AiKnowledgeTransfer.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class EntityFrameworkAuditLog : IAuditLog
{
    private readonly KnowledgeTransferDbContext _dbContext;

    public EntityFrameworkAuditLog(KnowledgeTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await _dbContext.AuditEvents.AddAsync(ToRecord(auditEvent), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditEvent>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditEvents.AsNoTracking();
        if (projectId is not null)
        {
            query = query.Where(auditEvent => auditEvent.ProjectId == projectId);
        }

        var records = await query.ToArrayAsync(cancellationToken);

        return records
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Select(ToDomain)
            .ToArray();
    }

    private static AuditEventRecord ToRecord(AuditEvent auditEvent)
    {
        return new AuditEventRecord
        {
            Id = auditEvent.Id,
            ProjectId = auditEvent.ProjectId,
            Action = auditEvent.Action,
            Actor = auditEvent.Actor,
            TargetType = auditEvent.TargetType,
            TargetId = auditEvent.TargetId,
            Summary = auditEvent.Summary,
            OccurredAt = auditEvent.OccurredAt
        };
    }

    private static AuditEvent ToDomain(AuditEventRecord record)
    {
        return new AuditEvent(
            record.Id,
            record.ProjectId,
            record.Action,
            record.Actor,
            record.TargetType,
            record.TargetId,
            record.Summary,
            record.OccurredAt);
    }
}
