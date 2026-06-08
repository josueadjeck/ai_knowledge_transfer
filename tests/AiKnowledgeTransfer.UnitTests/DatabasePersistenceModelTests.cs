using AiKnowledgeTransfer.Infrastructure.Persistence.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace AiKnowledgeTransfer.UnitTests;

public sealed class DatabasePersistenceModelTests
{
    [Fact]
    public void KnowledgeTransferDbContext_exposes_initial_relational_model()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new KnowledgeTransferDbContext(options);
        context.Database.EnsureCreated();

        Assert.NotNull(context.Model.FindEntityType(typeof(ProjectRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(DocumentRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(KnowledgeItemRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(KnowledgeReviewHistoryRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(RoadmapRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AuditEventRecord)));
    }
}
