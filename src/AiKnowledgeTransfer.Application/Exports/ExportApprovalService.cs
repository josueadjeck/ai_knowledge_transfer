namespace AiKnowledgeTransfer.Application.Exports;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Exports;

public sealed class ExportApprovalService(
    IProjectRepository projects,
    IAuditLog auditLog)
{
    public async Task<ExportApprovalResponse?> ApproveAsync(
        Guid projectId,
        ApproveExportRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var summary = $"Export '{request.FileName.Trim()}' was approved. {request.Comment.Trim()}";
        var auditEvent = AuditEvent.Create(
            project.Id,
            "ExportApproved",
            request.Reviewer,
            request.FileName,
            project.Id,
            summary);

        await auditLog.AppendAsync(auditEvent, cancellationToken);

        return ToResponse(project.Id, auditEvent);
    }

    public async Task<IReadOnlyCollection<ExportApprovalResponse>> ListAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var events = await auditLog.ListAsync(projectId, cancellationToken);
        return events
            .Where(auditEvent => auditEvent.Action == "ExportApproved")
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Select(auditEvent => ToResponse(projectId, auditEvent))
            .ToArray();
    }

    private static ExportApprovalResponse ToResponse(Guid projectId, AuditEvent auditEvent)
    {
        return new ExportApprovalResponse(
            auditEvent.Id,
            projectId,
            string.IsNullOrWhiteSpace(auditEvent.TargetType) ? "knowledge-transfer.md" : auditEvent.TargetType,
            auditEvent.Actor,
            auditEvent.Summary,
            auditEvent.OccurredAt);
    }
}
