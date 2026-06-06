namespace AiKnowledgeTransfer.Infrastructure;

using AiKnowledgeTransfer.Application.Abstractions;
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
        services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storageRootPath));
        services.AddSingleton<IDocumentParser, PlainTextDocumentParser>();
        services.AddHttpClient();
        services.AddSingleton<IKnowledgeExtractor>(serviceProvider =>
        {
            var heuristicExtractor = new HeuristicKnowledgeExtractor();
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return heuristicExtractor;
            }

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
