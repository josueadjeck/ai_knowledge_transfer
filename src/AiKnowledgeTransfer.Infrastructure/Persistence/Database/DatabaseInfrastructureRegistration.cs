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
}
