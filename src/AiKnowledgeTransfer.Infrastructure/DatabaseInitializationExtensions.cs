namespace AiKnowledgeTransfer.Infrastructure;

public static class DatabaseInitializationExtensions
{
    public static Task InitializeInfrastructureDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        return Persistence.Database.DatabaseInitializationExtensions.InitializeDatabaseAsync(services, cancellationToken);
    }
}
