namespace AiKnowledgeTransfer.Application;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Exports;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Roadmaps;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProjectService>();
        services.AddScoped<DocumentAnalysisService>();
        services.AddScoped<MarkdownExportService>();
        services.AddScoped<KnowledgeExtractionService>();
        services.AddScoped<KnowledgeReviewService>();
        services.AddScoped<RoadmapService>();

        return services;
    }
}
