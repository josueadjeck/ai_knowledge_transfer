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
    public async Task InitializeDatabaseAsync_applies_sqlite_migrations_when_configured()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-db-init-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "data", "knowledge-transfer.db");
        var services = new ServiceCollection();
        services.AddSingleton(new DatabasePersistenceOptions(
            PersistenceProvider.Database,
            $"Data Source={databasePath}",
            "Sqlite",
            "Migrations"));
        services.AddDatabasePersistence(builder => builder.UseSqlite($"Data Source={databasePath}"));

        await using var provider = services.BuildServiceProvider();

        await provider.InitializeDatabaseAsync(CancellationToken.None);

        Assert.True(File.Exists(databasePath));
        await using var context = provider.GetRequiredService<KnowledgeTransferDbContext>();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20260609000100_InitialCreate", appliedMigrations);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_skips_json_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DatabasePersistenceOptions(PersistenceProvider.Json, null, null));
        await using var provider = services.BuildServiceProvider();

        await provider.InitializeDatabaseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_rejects_unsupported_schema_mode()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DatabasePersistenceOptions(
            PersistenceProvider.Database,
            "Data Source=:memory:",
            "Sqlite",
            "ManualSql"));
        services.AddDatabasePersistence(builder => builder.UseSqlite("Data Source=:memory:"));

        await using var provider = services.BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.InitializeDatabaseAsync(CancellationToken.None));

        Assert.Contains("Unsupported AKT_DB_SCHEMA_MODE", exception.Message);
    }
}
