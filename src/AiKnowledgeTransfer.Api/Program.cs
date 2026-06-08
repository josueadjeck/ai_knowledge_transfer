using AiKnowledgeTransfer.Api;
using AiKnowledgeTransfer.Application;
using AiKnowledgeTransfer.Application.Audit;
using AiKnowledgeTransfer.Application.Compliance;
using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Exports;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Operations;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Application.Traceability;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Contracts.Validation;
using AiKnowledgeTransfer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var storageRootPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");

builder.Services
    .AddApplication()
    .AddInfrastructure(storageRootPath);

var app = builder.Build();

await app.Services.InitializeInfrastructureDatabaseAsync();

app.UseHttpsRedirection();

var projects = app.MapGroup("/api/projects");

projects.MapGet("/", async Task<IResult> (ProjectService service, CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(cancellationToken);
    return Results.Ok(result);
});

projects.MapGet("/{projectId:guid}", async Task<IResult> (
    Guid projectId,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapPost("/", async Task<IResult> (
    CreateProjectRequest request,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/projects/{result.Id}", result);
});

projects.MapPost("/{projectId:guid}/documents", async Task<IResult> (
    Guid projectId,
    RegisterDocumentRequest request,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.RegisterDocumentAsync(projectId, request, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Created($"/api/projects/{projectId}/documents/{result.Id}", result);
});

projects.MapPost("/{projectId:guid}/documents/upload", async Task<IResult> (
    Guid projectId,
    [FromForm] IFormFile? file,
    [FromForm] string? source,
    ProjectService service,
    CancellationToken cancellationToken) =>
{
    if (file is null)
    {
        return ApiResponses.BadRequest("Uploaded file is required.");
    }

    if (file.Length == 0)
    {
        return ApiResponses.BadRequest("Uploaded file is empty.");
    }

    var errors = DocumentFileValidation.Validate(file.FileName, file.ContentType, file.Length);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
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

    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Created($"/api/projects/{projectId}/documents/{result.Document.Id}", result);
})
.DisableAntiforgery();

projects.MapPost("/{projectId:guid}/documents/{documentId:guid}/analyze", async Task<IResult> (
    Guid projectId,
    Guid documentId,
    DocumentAnalysisService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.AnalyzeAsync(projectId, documentId, cancellationToken);
        return result is null ? ApiResponses.NotFound("Project or document was not found.") : Results.Ok(result);
    }
    catch (NotSupportedException exception)
    {
        return ApiResponses.BadRequest(exception.Message);
    }
    catch (InvalidOperationException exception)
    {
        return ApiResponses.BadRequest(exception.Message);
    }
});

projects.MapPost("/{projectId:guid}/documents/{documentId:guid}/extract-knowledge", async Task<IResult> (
    Guid projectId,
    Guid documentId,
    KnowledgeExtractionService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.ExtractAsync(projectId, documentId, cancellationToken);
        return result is null ? ApiResponses.NotFound("Project or document was not found.") : Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return ApiResponses.BadRequest(exception.Message);
    }
});

projects.MapPost("/{projectId:guid}/knowledge-items/{knowledgeItemId:guid}/submit-review", async Task<IResult> (
    Guid projectId,
    Guid knowledgeItemId,
    ReviewKnowledgeItemRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.SubmitForReviewAsync(projectId, knowledgeItemId, request, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project or knowledge item was not found.") : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/{knowledgeItemId:guid}/approve", async Task<IResult> (
    Guid projectId,
    Guid knowledgeItemId,
    ReviewKnowledgeItemRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.ApproveAsync(projectId, knowledgeItemId, request, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project or knowledge item was not found.") : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/{knowledgeItemId:guid}/reject", async Task<IResult> (
    Guid projectId,
    Guid knowledgeItemId,
    ReviewKnowledgeItemRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.RejectAsync(projectId, knowledgeItemId, request, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project or knowledge item was not found.") : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/bulk-submit-review", async Task<IResult> (
    Guid projectId,
    BulkReviewKnowledgeItemsRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.SubmitManyForReviewAsync(
        projectId,
        request.KnowledgeItemIds,
        ToReviewRequest(request),
        cancellationToken);

    return result.Count == 0 ? ApiResponses.NotFound("Project or knowledge items were not found.") : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/bulk-approve", async Task<IResult> (
    Guid projectId,
    BulkReviewKnowledgeItemsRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.ApproveManyAsync(
        projectId,
        request.KnowledgeItemIds,
        ToReviewRequest(request),
        cancellationToken);

    return result.Count == 0 ? ApiResponses.NotFound("Project or knowledge items were not found.") : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/knowledge-items/bulk-reject", async Task<IResult> (
    Guid projectId,
    BulkReviewKnowledgeItemsRequest request,
    KnowledgeReviewService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.RejectManyAsync(
        projectId,
        request.KnowledgeItemIds,
        ToReviewRequest(request),
        cancellationToken);

    return result.Count == 0 ? ApiResponses.NotFound("Project or knowledge items were not found.") : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/knowledge-items/review-summary", async Task<IResult> (
    Guid projectId,
    KnowledgeReviewSummaryService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapPost("/{projectId:guid}/roadmaps", async Task<IResult> (
    Guid projectId,
    GenerateRoadmapRequest request,
    RoadmapService service,
    CancellationToken cancellationToken) =>
{
    var errors = RequestValidation.Validate(request);
    if (errors.Count > 0)
    {
        return ApiResponses.ValidationProblem(errors);
    }

    var result = await service.GenerateAsync(projectId, request, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Created($"/api/projects/{projectId}/roadmaps/{result.Id}", result);
});

projects.MapGet("/{projectId:guid}/onboarding-readiness", async Task<IResult> (
    Guid projectId,
    OnboardingReadinessService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/onboarding-start-package", async Task<IResult> (
    Guid projectId,
    OnboardingStartPackageService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/exports/markdown", async Task<IResult> (
    Guid projectId,
    MarkdownExportService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ExportProjectAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/traceability", async Task<IResult> (
    Guid projectId,
    TraceabilityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetMatrixAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/compliance", async Task<IResult> (
    Guid projectId,
    ComplianceMatrixService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetMatrixAsync(projectId, cancellationToken);
    return result is null ? ApiResponses.NotFound("Project was not found.") : Results.Ok(result);
});

projects.MapGet("/{projectId:guid}/audit", async Task<IResult> (
    Guid projectId,
    AuditLogService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(projectId, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/health", (OperationalHealthService service) =>
{
    return Results.Ok(service.GetStatus("AiKnowledgeTransfer.Api"));
})
.WithName("Health");

app.MapGet("/api/document-parsers", (DocumentParserCapabilityService service) =>
{
    return Results.Ok(service.List());
});

app.MapGet("/api/security/roles", (RolePermissionService service) =>
{
    return Results.Ok(service.GetMatrix());
});

app.MapGet("/api/audit", async Task<IResult> (
    AuditLogService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(projectId: null, cancellationToken);
    return Results.Ok(result);
});

var operations = app.MapGroup("/api/operations");

operations.MapPost("/backups", async Task<IResult> (
    PersistenceBackupService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(cancellationToken);
    return Results.Created($"/api/operations/backups/{result.FileName}", result);
});

operations.MapGet("/backups", (PersistenceBackupService service) =>
{
    return Results.Ok(service.List());
});

operations.MapGet("/backups/{fileName}/preview", async Task<IResult> (
    string fileName,
    PersistenceBackupService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.PreviewAsync(fileName, cancellationToken);
        return result is null ? ApiResponses.NotFound("Backup was not found.") : Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return ApiResponses.BadRequest(exception.Message);
    }
});

operations.MapPost("/backups/{fileName}/restore", async Task<IResult> (
    string fileName,
    PersistenceBackupService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await service.RestoreAsync(fileName, cancellationToken);
        return result is null ? ApiResponses.NotFound("Backup was not found.") : Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return ApiResponses.BadRequest(exception.Message);
    }
});

app.Run();

static ReviewKnowledgeItemRequest ToReviewRequest(BulkReviewKnowledgeItemsRequest request)
{
    return new ReviewKnowledgeItemRequest(request.Reviewer, request.Comment, request.QualityStatus);
}
