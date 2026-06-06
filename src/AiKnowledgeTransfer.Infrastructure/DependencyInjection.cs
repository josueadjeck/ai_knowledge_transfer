namespace AiKnowledgeTransfer.Infrastructure;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Infrastructure.Knowledge;
using AiKnowledgeTransfer.Infrastructure.Parsing;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string storageRootPath)
    {
        services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storageRootPath));
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddSingleton<IKnowledgeExtractor, HeuristicKnowledgeExtractor>();
        return services;
    }
}
