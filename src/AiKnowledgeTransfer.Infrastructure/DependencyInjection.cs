namespace AiKnowledgeTransfer.Infrastructure;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Infrastructure.Audit;
using AiKnowledgeTransfer.Infrastructure.Knowledge;
using AiKnowledgeTransfer.Infrastructure.Parsing;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string storageRootPath)
    {
        var appDataPath = Path.GetFullPath(Path.Combine(storageRootPath, ".."));
        var uploadStoragePath = Path.GetFullPath(storageRootPath);
        var projectStorePath = Path.Combine(appDataPath, "projects.json");
        var auditLogPath = Path.Combine(appDataPath, "audit-log.json");

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gpt-5.4-mini";
        }

        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.openai.com/v1/";
        }

        services.AddSingleton(new OperationalHealthOptions(
            appDataPath,
            uploadStoragePath,
            projectStorePath,
            auditLogPath,
            "OpenAI",
            model,
            baseUrl,
            !string.IsNullOrWhiteSpace(apiKey)));

        services.AddSingleton<IProjectRepository>(_ => new JsonProjectRepository(projectStorePath));
        services.AddSingleton<IAuditLog>(_ => new JsonAuditLog(auditLogPath));
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(uploadStoragePath));
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddHttpClient();
        services.AddSingleton<IKnowledgeExtractor>(serviceProvider =>
        {
            var heuristicExtractor = new HeuristicKnowledgeExtractor();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return heuristicExtractor;
            }

            var options = new OpenAiKnowledgeExtractorOptions(
                apiKey,
                model,
                new Uri(baseUrl, UriKind.Absolute));

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<FallbackKnowledgeExtractor>>();
            var openAiExtractor = new OpenAiKnowledgeExtractor(httpClientFactory.CreateClient(), options);

            return new FallbackKnowledgeExtractor(openAiExtractor, heuristicExtractor, logger);
        });
        return services;
    }
}
