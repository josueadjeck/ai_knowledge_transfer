using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public static class DatabaseInfrastructureRegistration
{
    public static IServiceCollection AddDatabasePersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<KnowledgeTransferDbContext>(configureDbContext);
        return services;
    }

    public static IServiceCollection AddConfiguredDatabasePersistence(
        this IServiceCollection services,
        DatabasePersistenceOptions options)
    {
        if (options.Provider != PersistenceProvider.Database)
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("AKT_DB_CONNECTION_STRING is required when AKT_PERSISTENCE_PROVIDER=Database.");
        }

        if (options.ProviderName?.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            return services.AddDatabasePersistence(builder => builder.UseSqlite(options.ConnectionString));
        }

        if (options.ProviderName?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            return services.AddDatabasePersistence(builder => builder.UseSqlServer(options.ConnectionString));
        }

        throw new InvalidOperationException("Unsupported AKT_DB_PROVIDER. Currently supported: Sqlite, SqlServer.");
    }
}
