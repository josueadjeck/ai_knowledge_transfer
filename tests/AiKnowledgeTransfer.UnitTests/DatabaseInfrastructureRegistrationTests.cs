using AiKnowledgeTransfer.Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class DatabaseInfrastructureRegistrationTests
{
    [Fact]
    public void AddConfiguredDatabasePersistence_registers_sql_server_provider()
    {
        var services = new ServiceCollection();
        services.AddConfiguredDatabasePersistence(new DatabasePersistenceOptions(
            PersistenceProvider.Database,
            "Server=localhost;Database=AiKnowledgeTransfer;User Id=sa;Password=example;",
            "SqlServer",
            "Migrations"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KnowledgeTransferDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Fact]
    public void AddConfiguredDatabasePersistence_rejects_unknown_database_provider()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConfiguredDatabasePersistence(new DatabasePersistenceOptions(
                PersistenceProvider.Database,
                "Host=localhost;Database=AiKnowledgeTransfer",
                "Postgres",
                "Migrations")));

        Assert.Contains("Sqlite, SqlServer", exception.Message, StringComparison.Ordinal);
    }
}
