namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed record DatabasePersistenceOptions(
    PersistenceProvider Provider,
    string? ConnectionString,
    string? ProviderName);
