using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DatabasePersistenceOptions>();
        if (options.Provider != PersistenceProvider.Database)
        {
            return;
        }

        EnsureSqliteDirectory(options);

        var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeTransferDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static void EnsureSqliteDirectory(DatabasePersistenceOptions options)
    {
        if (options.ProviderName?.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) != true
            || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return;
        }

        var builder = new SqliteConnectionStringBuilder(options.ConnectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
