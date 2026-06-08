namespace AiKnowledgeTransfer.Application.Exports;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Exports;

public sealed class ExportHistoryService(IAuditLog auditLog)
{
    public async Task<IReadOnlyCollection<ExportHistoryResponse>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var events = await auditLog.ListAsync(projectId, cancellationToken);
        return events
            .Where(auditEvent => auditEvent.Action == "ProjectExported")
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Select(auditEvent => new ExportHistoryResponse(
                auditEvent.Id,
                projectId,
                string.IsNullOrWhiteSpace(auditEvent.TargetType) ? "knowledge-transfer.md" : auditEvent.TargetType,
                "text/markdown",
                auditEvent.Summary,
                auditEvent.OccurredAt))
            .ToArray();
    }
}
