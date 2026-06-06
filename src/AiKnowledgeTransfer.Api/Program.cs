using AiKnowledgeTransfer.Application;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Exports;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Application.Traceability;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var storageRootPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");

builder.Services
    .AddApplication()
    .AddInfrastructure(storageRootPath);

var app = builder.Build();

app.UseHttpsRedirection();

var projects = app.MapGroup("/api/projects");

projects.MapGet("/", async (ProjectService service, CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(cancellationToken);
    return Results.Ok(result);
});

projects.MapGet("/{projectId:guid}", async (
    Guid projectId,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetAsync(projectId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
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

projects.MapPost("/{projectId:guid}/documents/upload", async (
    Guid projectId,
    [FromForm] IFormFile file,
    [FromForm] string? source,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest("Uploaded file is empty.");
    }

    await using var stream = file.OpenReadStream();
    var result = await service.UploadDocumentAsync(
        projectId,
        new UploadDocumentCommand(
            file.FileName,
            file.ContentType,
            string.IsNullOrWhiteSpace(source) ? "Manual upload" : source,
            stream),
        cancellationToken);

    return result is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/documents/{result.Document.Id}", result);
})
.DisableAntiforgery();

projects.MapPost("/{projectId:guid}/documents/{documentId:guid}/analyze", async (
    Guid projectId,
    Guid documentId,
    DocumentAnalysisService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.AnalyzeAsync(projectId, documentId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (NotSupportedException exception)
    {
        return Results.BadRequest(exception.Message);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(exception.Message);
    }
});

projects.MapPost("/{projectId:guid}/documents/{documentId:guid}/extract-knowledge", async (
    Guid projectId,
    Guid documentId,
    KnowledgeExtractionService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.ExtractAsync(projectId, documentId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(exception.Message);
    }
});

projects.MapPost("/{projectId:guid}/knowledge-items/{knowledgeItemId:guid}/submit-review", async (
    Guid projectId,
    Guid knowledgeItemId,
    ReviewKnowledgeItemRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.SubmitForReviewAsync(projectId, knowledgeItemId, request, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/{knowledgeItemId:guid}/approve", async (
    Guid projectId,
    Guid knowledgeItemId,
    ReviewKnowledgeItemRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ApproveAsync(projectId, knowledgeItemId, request, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/{knowledgeItemId:guid}/reject", async (
    Guid projectId,
    Guid knowledgeItemId,
    ReviewKnowledgeItemRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RejectAsync(projectId, knowledgeItemId, request, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
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

projects.MapGet("/{projectId:guid}/exports/markdown", async (
    Guid projectId,
    MarkdownExportService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ExportProjectAsync(projectId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/traceability", async (
    Guid projectId,
    TraceabilityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetMatrixAsync(projectId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
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
