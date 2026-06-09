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

    [Theory]
    [InlineData("Postgres")]
    [InlineData("PostgreSql")]
    [InlineData("PostgreSQL")]
    public void AddConfiguredDatabasePersistence_registers_postgres_provider(string providerName)
    {
        var services = new ServiceCollection();
        services.AddConfiguredDatabasePersistence(new DatabasePersistenceOptions(
            PersistenceProvider.Database,
            "Host=localhost;Database=AiKnowledgeTransfer;Username=akt;Password=example",
            providerName,
            "Migrations"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KnowledgeTransferDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [Fact]
    public void AddConfiguredDatabasePersistence_rejects_unknown_database_provider()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddConfiguredDatabasePersistence(new DatabasePersistenceOptions(
                PersistenceProvider.Database,
                "Host=localhost;Database=AiKnowledgeTransfer",
                "Oracle",
                "Migrations")));

        Assert.Contains("Sqlite, SqlServer, Postgres", exception.Message, StringComparison.Ordinal);
    }
}
