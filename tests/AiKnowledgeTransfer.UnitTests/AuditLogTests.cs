namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Audit;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Infrastructure.Audit;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Storage;

public sealed class AuditLogTests
{
    [Fact]
    public async Task JsonAuditLog_persists_events_for_new_instance()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"), "audit-log.json");
        var auditLog = new JsonAuditLog(filePath);
        var projectId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        await auditLog.AppendAsync(
            AuditEvent.Create(projectId, "DocumentUploaded", "tester", "Document", targetId, "Document was uploaded."),
            CancellationToken.None);

        var reloadedAuditLog = new JsonAuditLog(filePath);
        var events = await reloadedAuditLog.ListAsync(projectId, CancellationToken.None);

        var auditEvent = Assert.Single(events);
        Assert.Equal("DocumentUploaded", auditEvent.Action);
        Assert.Equal(targetId, auditEvent.TargetId);
    }

    [Fact]
    public async Task ProjectService_writes_audit_events_for_create_and_upload()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(storageRoot, "uploads"));
        var auditLog = new JsonAuditLog(Path.Combine(storageRoot, "audit-log.json"));
        var projectService = new ProjectService(repository, storage, auditLog);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Audit Project", "Audit test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("Audit document"u8.ToArray());
        await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("audit.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        var auditService = new AuditLogService(auditLog);
        var events = await auditService.ListAsync(project.Id, CancellationToken.None);

        Assert.Contains(events, auditEvent => auditEvent.Action == "ProjectCreated");
        Assert.Contains(events, auditEvent => auditEvent.Action == "DocumentUploaded");
    }
}
