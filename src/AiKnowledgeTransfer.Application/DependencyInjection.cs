namespace AiKnowledgeTransfer.Application;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Roadmaps;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProjectService>();
        services.AddScoped<RoadmapService>();

        return services;
    }
}
