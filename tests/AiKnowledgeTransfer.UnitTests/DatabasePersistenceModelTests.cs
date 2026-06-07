using AiKnowledgeTransfer.Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
namespace AiKnowledgeTransfer.UnitTests;

public sealed class DatabasePersistenceModelTests
{
    [Fact]
    public void KnowledgeTransferDbContext_exposes_initial_relational_model()
    {
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var context = new KnowledgeTransferDbContext(options);

        Assert.NotNull(context.Model.FindEntityType(typeof(ProjectRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(DocumentRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(KnowledgeItemRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(RoadmapRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AuditEventRecord)));
    }
}
