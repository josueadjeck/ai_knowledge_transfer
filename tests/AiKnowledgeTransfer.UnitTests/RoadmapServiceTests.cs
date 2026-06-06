namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Infrastructure.Knowledge;
using AiKnowledgeTransfer.Infrastructure.Parsing;
using AiKnowledgeTransfer.Infrastructure.Storage;
using AiKnowledgeTransfer.Infrastructure.Persistence;

public sealed class RoadmapServiceTests
{
    [Fact]
    public async Task UploadDocumentAsync_stores_file_and_registers_document_version()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var projectService = new ProjectService(repository, storage);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Upload Project", "Document storage test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("System architecture overview"u8.ToArray());
        var result = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand(
                "architecture.md",
                "text/markdown",
                "Manual upload",
                content),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("architecture.md", result.Document.FileName);
        Assert.Equal("text/markdown", result.Document.ContentType);
        Assert.Equal("Registered", result.Document.Status);
        Assert.True(File.Exists(Path.Combine(storageRoot, result.Document.StoragePath)));
    }

    [Fact]
    public async Task ExtractAsync_creates_knowledge_items_from_analyzed_document()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Extraction Project", "Knowledge extraction test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            M3 Platform coordinates onboarding for Support Engineers.

            Deployment and Recovery workflow must be reviewed by senior engineers.

            CTU communication details are unknown and remain an open question.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand(
                "knowledge.md",
                "text/markdown",
                "Manual upload",
                content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);

        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        Assert.True(extraction.CreatedItemCount >= 3);
        Assert.Contains(extraction.KnowledgeItems, item => item.Type == "GlossaryTerm" && item.Title == "CTU");
        Assert.Contains(extraction.KnowledgeItems, item => item.Type == "Workflow");
        Assert.Contains(extraction.KnowledgeItems, item => item.Type == "OpenQuestion");
    }

    [Fact]
    public async Task KnowledgeReviewService_updates_status_and_review_metadata()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Review Project", "Knowledge review test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("review.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var itemId = extraction.KnowledgeItems.First().Id;

        var inReview = await reviewService.SubmitForReviewAsync(
            project.Id,
            itemId,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Needs technical confirmation."),
            CancellationToken.None);

        Assert.NotNull(inReview);
        Assert.Equal("InReview", inReview.ReviewStatus);

        var approved = await reviewService.ApproveAsync(
            project.Id,
            itemId,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Confirmed against source."),
            CancellationToken.None);

        Assert.NotNull(approved);
        Assert.Equal("Approved", approved.ReviewStatus);
        Assert.Equal("Senior Engineer", approved.ReviewedBy);
        Assert.Equal("Confirmed against source.", approved.ReviewComment);
        Assert.NotNull(approved.ReviewedAt);
    }

    [Fact]
    public async Task AnalyzeAsync_parses_uploaded_text_document_into_chunks()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Parsing Project", "Document parsing test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            System Overview

            The M3 platform coordinates technical onboarding.

            Deployment and recovery workflows must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand(
                "system-overview.md",
                "text/markdown",
                "Manual upload",
                content),
            CancellationToken.None);

        Assert.NotNull(upload);

        var analysis = await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(analysis);
        Assert.Equal("Analyzed", analysis.Status);
        Assert.Equal(3, analysis.ChunkCount);
        Assert.Contains(analysis.Chunks, chunk => chunk.Text.Contains("Deployment", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_creates_requested_number_of_weeks()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests"));
        var projectService = new ProjectService(repository, storage);
        var roadmapService = new RoadmapService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("M3 Platform", "Technical onboarding", "Engineering"),
            CancellationToken.None);

        await projectService.RegisterDocumentAsync(
            project.Id,
            new RegisterDocumentRequest("sad.md", "text/markdown", "Architecture repository", 2048),
            CancellationToken.None);

        var roadmap = await roadmapService.GenerateAsync(
            project.Id,
            new GenerateRoadmapRequest("Support Engineer", 4),
            CancellationToken.None);

        Assert.NotNull(roadmap);
        Assert.Equal("Support Engineer", roadmap.TargetRole);
        Assert.Equal(4, roadmap.Weeks.Count);
        Assert.Equal([1, 2, 3, 4], roadmap.Weeks.Select(week => week.WeekNumber));
    }
}
