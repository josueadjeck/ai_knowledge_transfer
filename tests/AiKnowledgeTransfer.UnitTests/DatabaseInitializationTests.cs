using AiKnowledgeTransfer.Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class DatabaseInitializationTests
{
    [Fact]
    public async Task InitializeDatabaseAsync_creates_sqlite_database_file_and_schema()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-db-init-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "data", "knowledge-transfer.db");
        var services = new ServiceCollection();
        services.AddSingleton(new DatabasePersistenceOptions(
            PersistenceProvider.Database,
            $"Data Source={databasePath}",
            "Sqlite"));
        services.AddDatabasePersistence(builder => builder.UseSqlite($"Data Source={databasePath}"));

        await using var provider = services.BuildServiceProvider();

        await provider.InitializeDatabaseAsync(CancellationToken.None);

        Assert.True(File.Exists(databasePath));
        await using var context = provider.GetRequiredService<KnowledgeTransferDbContext>();
        Assert.True(await context.Database.CanConnectAsync());
    }

    [Fact]
    public async Task InitializeDatabaseAsync_skips_json_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DatabasePersistenceOptions(PersistenceProvider.Json, null, null));
        await using var provider = services.BuildServiceProvider();

        await provider.InitializeDatabaseAsync(CancellationToken.None);
    }
}
