namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Exports;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Application.Traceability;
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
    public async Task UploadDocumentAsync_rejects_unsupported_file_type()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var projectService = new ProjectService(repository, storage);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Upload Project", "Document storage test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("binary"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(() => projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand(
                "tool.exe",
                "application/octet-stream",
                "Manual upload",
                content),
            CancellationToken.None));

        Assert.False(Directory.Exists(storageRoot));
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
        Assert.All(extraction.KnowledgeItems, item => Assert.Equal("Heuristic", item.ExtractionProvider));
        Assert.All(extraction.KnowledgeItems, item => Assert.False(item.ExtractionUsedFallback));

        var openQuestion = extraction.KnowledgeItems.First(item => item.Type == "OpenQuestion");
        Assert.Equal("Uncertain", openQuestion.ExtractionQuality);
        Assert.NotNull(openQuestion.SourceChunkNumber);
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
            new ReviewKnowledgeItemRequest("Senior Engineer", "Needs technical confirmation.", "NeedsClarification"),
            CancellationToken.None);

        Assert.NotNull(inReview);
        Assert.Equal("InReview", inReview.ReviewStatus);
        Assert.Equal("NeedsClarification", inReview.ExtractionQuality);

        var approved = await reviewService.ApproveAsync(
            project.Id,
            itemId,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Confirmed against source.", "Verified"),
            CancellationToken.None);

        Assert.NotNull(approved);
        Assert.Equal("Approved", approved.ReviewStatus);
        Assert.Equal("Senior Engineer", approved.ReviewedBy);
        Assert.Equal("Confirmed against source.", approved.ReviewComment);
        Assert.Equal("Verified", approved.ExtractionQuality);
        Assert.NotNull(approved.ReviewedAt);
    }

    [Fact]
    public async Task KnowledgeReviewService_bulk_approves_items_with_verified_quality()
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
            new CreateProjectRequest("Bulk Review Project", "Bulk review test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("bulk-review.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var itemIds = extraction.KnowledgeItems.Take(2).Select(item => item.Id).ToArray();

        var updated = await reviewService.ApproveManyAsync(
            project.Id,
            itemIds,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Bulk confirmed."),
            CancellationToken.None);

        Assert.Equal(itemIds.Length, updated.Count);
        Assert.All(updated, item =>
        {
            Assert.Equal("Approved", item.ReviewStatus);
            Assert.Equal("Verified", item.ExtractionQuality);
            Assert.Equal("Senior Engineer", item.ReviewedBy);
        });
    }

    [Fact]
    public async Task KnowledgeReviewSummaryService_counts_review_quality_type_and_final_items()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var summaryService = new KnowledgeReviewSummaryService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Review Summary Project", "Review statistics test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("review-summary.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var workflow = extraction.KnowledgeItems.First(item => item.Type == "Workflow");
        var openQuestion = extraction.KnowledgeItems.First(item => item.Type == "OpenQuestion");

        await reviewService.ApproveAsync(
            project.Id,
            workflow.Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Workflow confirmed."),
            CancellationToken.None);

        await reviewService.SubmitForReviewAsync(
            project.Id,
            openQuestion.Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Open question needs clarification."),
            CancellationToken.None);

        var summary = await summaryService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.True(summary.TotalCount >= extraction.KnowledgeItems.Count);
        Assert.Equal(1, summary.FinalCount);
        Assert.Equal(1, CountFor(summary.ByReviewStatus, "Approved"));
        Assert.Equal(1, CountFor(summary.ByReviewStatus, "InReview"));
        Assert.Equal(1, CountFor(summary.ByQualityStatus, "Verified"));
        Assert.Equal(1, CountFor(summary.ByQualityStatus, "NeedsClarification"));
        Assert.True(CountFor(summary.ByType, "Workflow") >= 1);
        Assert.True(CountFor(summary.ByType, "OpenQuestion") >= 1);
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
        Assert.All(analysis.Chunks, chunk => Assert.Equal(upload.Document.Id, chunk.DocumentId));
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

    [Fact]
    public async Task GenerateAsync_includes_approved_knowledge_and_review_notes()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Roadmap Review Project", "Approved knowledge roadmap test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("roadmap.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var workflow = extraction.KnowledgeItems.First(item => item.Type == "Workflow");
        await reviewService.ApproveAsync(
            project.Id,
            workflow.Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Workflow confirmed."),
            CancellationToken.None);

        var roadmap = await roadmapService.GenerateAsync(
            project.Id,
            new GenerateRoadmapRequest("Support Engineer", 3),
            CancellationToken.None);

        Assert.NotNull(roadmap);
        Assert.Contains(roadmap.Weeks.Single(week => week.WeekNumber == 3).LearningGoals, goal => goal.Contains(workflow.Title, StringComparison.Ordinal));
        Assert.Contains(roadmap.Weeks.SelectMany(week => week.ReviewNotes), note => note.Contains("Draft", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_keeps_approved_but_unverified_knowledge_in_review_notes()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Quality Roadmap Project", "Quality gated roadmap test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("Deployment workflow must be reviewed by senior engineers."u8.ToArray());
        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("quality-roadmap.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var workflow = extraction.KnowledgeItems.First(item => item.Type == "Workflow");
        await reviewService.ApproveAsync(
            project.Id,
            workflow.Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Approved but still needs clarification.", "NeedsClarification"),
            CancellationToken.None);

        var roadmap = await roadmapService.GenerateAsync(
            project.Id,
            new GenerateRoadmapRequest("Support Engineer", 3),
            CancellationToken.None);

        Assert.NotNull(roadmap);
        Assert.DoesNotContain(roadmap.Weeks.SelectMany(week => week.LearningGoals), goal => goal.Contains(workflow.Title, StringComparison.Ordinal));
        Assert.Contains(roadmap.Weeks.SelectMany(week => week.ReviewNotes), note => note.Contains("NeedsClarification", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportProjectAsync_creates_markdown_with_sources_knowledge_and_roadmap()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);
        var exportService = new MarkdownExportService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Export Project", "Markdown export test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("export.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var item = extraction.KnowledgeItems.First();
        await reviewService.ApproveAsync(
            project.Id,
            item.Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Confirmed for export."),
            CancellationToken.None);

        await roadmapService.GenerateAsync(project.Id, new GenerateRoadmapRequest("Support Engineer", 3), CancellationToken.None);

        var export = await exportService.ExportProjectAsync(project.Id, CancellationToken.None);

        Assert.NotNull(export);
        Assert.Equal("text/markdown", export.ContentType);
        Assert.Contains("# Export Project Knowledge Transfer", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Sources", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Knowledge Items", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Roadmaps", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Traceability Matrix", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("- Quality: Verified", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("Support Engineer", export.Markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMatrixAsync_links_source_knowledge_review_and_roadmap_usage()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);
        var traceabilityService = new TraceabilityService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Traceability Project", "Traceability matrix test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("traceability.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        var extraction = await extractionService.ExtractAsync(project.Id, upload.Document.Id, CancellationToken.None);

        Assert.NotNull(extraction);
        var workflow = extraction.KnowledgeItems.First(item => item.Type == "Workflow");
        await reviewService.ApproveAsync(
            project.Id,
            workflow.Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Workflow confirmed."),
            CancellationToken.None);

        await roadmapService.GenerateAsync(project.Id, new GenerateRoadmapRequest("Support Engineer", 3), CancellationToken.None);

        var matrix = await traceabilityService.GetMatrixAsync(project.Id, CancellationToken.None);

        Assert.NotNull(matrix);
        var workflowRow = matrix.Rows.Single(row => row.KnowledgeItemId == workflow.Id);
        Assert.Equal("traceability.md", workflowRow.DocumentName);
        Assert.Equal("Approved", workflowRow.KnowledgeReviewStatus);
        Assert.Equal("Verified", workflowRow.ExtractionQuality);
        Assert.Equal("Senior Engineer", workflowRow.ReviewedBy);
        Assert.True(workflowRow.RoadmapUsageCount > 0);
        Assert.True(workflowRow.IncludedInExport);
    }

    private static int CountFor(IReadOnlyCollection<KnowledgeReviewSummaryGroupResponse> groups, string name)
    {
        return groups.FirstOrDefault(group => group.Name == name)?.Count ?? 0;
    }
}
