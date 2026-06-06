using AiKnowledgeTransfer.Application;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.UseHttpsRedirection();

var projects = app.MapGroup("/api/projects");

projects.MapGet("/", async (ProjectService service, CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(cancellationToken);
    return Results.Ok(result);
});

projects.MapPost("/", async (
    CreateProjectRequest request,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/projects/{result.Id}", result);
});

projects.MapPost("/{projectId:guid}/documents", async (
    Guid projectId,
    RegisterDocumentRequest request,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RegisterDocumentAsync(projectId, request, cancellationToken);
    return result is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/documents/{result.Id}", result);
});

projects.MapPost("/{projectId:guid}/roadmaps", async (
    Guid projectId,
    GenerateRoadmapRequest request,
    RoadmapService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GenerateAsync(projectId, request, cancellationToken);
    return result is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/roadmaps/{result.Id}", result);
});

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "AiKnowledgeTransfer.Api",
        timestamp = DateTimeOffset.UtcNow
    });
})
.WithName("Health");

app.Run();
