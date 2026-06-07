using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Infrastructure.Persistence.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class EntityFrameworkAuditLogTests
{
    [Fact]
    public async Task Audit_log_persists_and_filters_events_by_project()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new KnowledgeTransferDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        await using (var writeContext = new KnowledgeTransferDbContext(options))
        {
            var auditLog = new EntityFrameworkAuditLog(writeContext);
            await auditLog.AppendAsync(
                AuditEvent.Create(projectId, "ProjectCreated", "system", "Project", projectId, "Project created."),
                CancellationToken.None);
            await auditLog.AppendAsync(
                AuditEvent.Create(otherProjectId, "DocumentUploaded", "system", "Document", Guid.NewGuid(), "Other project document."),
                CancellationToken.None);
        }

        await using (var readContext = new KnowledgeTransferDbContext(options))
        {
            var auditLog = new EntityFrameworkAuditLog(readContext);
            var projectEvents = await auditLog.ListAsync(projectId, CancellationToken.None);
            var allEvents = await auditLog.ListAsync(projectId: null, CancellationToken.None);

            Assert.Single(projectEvents);
            Assert.Equal("ProjectCreated", projectEvents.Single().Action);
            Assert.Equal(2, allEvents.Count);
        }
    }
}
