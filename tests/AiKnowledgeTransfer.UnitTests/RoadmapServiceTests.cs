namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Compliance;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Exports;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Application.Traceability;
using AiKnowledgeTransfer.Contracts.Exports;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Infrastructure.Knowledge;
using AiKnowledgeTransfer.Infrastructure.Parsing;
using AiKnowledgeTransfer.Infrastructure.Storage;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Audit;

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
    public async Task OnboardingReadiness_reports_not_ready_without_verified_knowledge_or_roadmap()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var projectService = new ProjectService(repository, storage);
        var readinessService = new OnboardingReadinessService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Readiness Project", "Onboarding readiness test", "Engineering"),
            CancellationToken.None);

        var readiness = await readinessService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(readiness);
        Assert.False(readiness.CanStartOnboarding);
        Assert.Equal("NotReady", readiness.Status);
        Assert.Contains(readiness.Recommendations, recommendation => recommendation.Contains("Roadmap", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnboardingReadiness_reports_ready_for_pilot_with_verified_knowledge_and_roadmap()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var projectService = new ProjectService(repository, storage);
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);
        var readinessService = new OnboardingReadinessService(repository);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Ready Project", "Onboarding readiness test", "Engineering"),
            CancellationToken.None);
        var details = await projectService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(details);
        await reviewService.ApproveAsync(
            project.Id,
            details.KnowledgeItems.First().Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Ready for pilot."),
            CancellationToken.None);
        await roadmapService.GenerateAsync(
            project.Id,
            new GenerateRoadmapRequest("Support Engineer", 2),
            CancellationToken.None);

        var readiness = await readinessService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(readiness);
        Assert.True(readiness.CanStartOnboarding);
        Assert.Equal("ReadyForPilot", readiness.Status);
        Assert.Equal(1, readiness.FinalKnowledgeCount);
        Assert.Equal(1, readiness.RoadmapCount);
        Assert.Equal("Support Engineer", readiness.LatestRoadmapTargetRole);
    }

    [Fact]
    public async Task OnboardingStartPackage_returns_readiness_task_when_project_is_not_ready()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var projectService = new ProjectService(repository, storage);
        var readinessService = new OnboardingReadinessService(repository);
        var startPackageService = new OnboardingStartPackageService(repository, readinessService);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Package Project", "Onboarding package test", "Engineering"),
            CancellationToken.None);

        var package = await startPackageService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(package);
        Assert.False(package.CanStartOnboarding);
        Assert.Equal("NotReady", package.Status);
        Assert.Empty(package.FirstWeeks);
        Assert.Contains(package.StarterTasks, task => task.Contains("Readiness", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnboardingStartPackage_returns_first_weeks_for_ready_pilot()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var projectService = new ProjectService(repository, storage);
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);
        var readinessService = new OnboardingReadinessService(repository);
        var startPackageService = new OnboardingStartPackageService(repository, readinessService);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Ready Package Project", "Onboarding package test", "Engineering"),
            CancellationToken.None);
        var details = await projectService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(details);
        await reviewService.ApproveAsync(
            project.Id,
            details.KnowledgeItems.First().Id,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Ready for package."),
            CancellationToken.None);
        await roadmapService.GenerateAsync(
            project.Id,
            new GenerateRoadmapRequest("Support Engineer", 3),
            CancellationToken.None);

        var package = await startPackageService.GetAsync(project.Id, CancellationToken.None);

        Assert.NotNull(package);
        Assert.True(package.CanStartOnboarding);
        Assert.Equal("ReadyForPilot", package.Status);
        Assert.Equal("Support Engineer", package.TargetRole);
        Assert.Equal(2, package.FirstWeeks.Count);
        Assert.NotEmpty(package.StarterTasks);
        Assert.NotEmpty(package.ReviewWarnings);
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
        Assert.Equal(2, approved.ReviewHistory.Count);
        Assert.Contains(approved.ReviewHistory, history =>
            history.Action == "SubmitForReview"
            && history.ReviewStatus == "InReview"
            && history.QualityStatus == "NeedsClarification");
        Assert.Contains(approved.ReviewHistory, history =>
            history.Action == "Approve"
            && history.ReviewStatus == "Approved"
            && history.QualityStatus == "Verified");
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
        Assert.Equal(nameof(PlainTextDocumentParser), analysis.ParserName);
        Assert.Contains("Text parser created", analysis.Detail, StringComparison.Ordinal);
        Assert.All(analysis.Chunks, chunk => Assert.Equal(upload.Document.Id, chunk.DocumentId));
        Assert.All(analysis.Chunks, chunk => Assert.Contains("Plain text body", chunk.SourceReference, StringComparison.Ordinal));
        Assert.All(analysis.Chunks, chunk => Assert.Contains("Text", chunk.QualityStatus, StringComparison.Ordinal));
        Assert.Contains(analysis.QualitySummary, quality => quality.QualityStatus == "UsableText");
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
        var auditLog = new JsonAuditLog(Path.Combine(storageRoot, "audit-log.json"));
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var roadmapService = new RoadmapService(repository);
        var exportService = new MarkdownExportService(repository, auditLog);
        var exportHistoryService = new ExportHistoryService(auditLog);
        var exportApprovalService = new ExportApprovalService(repository, auditLog);

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
        Assert.Contains("## Review History", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Traceability Matrix", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("- Quality: Verified", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("| Time | Action | Status | Quality | Reviewer | Comment |", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("Approve", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("Review History", export.Markdown, StringComparison.Ordinal);
        Assert.Contains("Support Engineer", export.Markdown, StringComparison.Ordinal);

        var history = await exportHistoryService.ListAsync(project.Id, CancellationToken.None);

        var exportRun = Assert.Single(history);
        Assert.Equal(export.FileName, exportRun.FileName);
        Assert.Equal("text/markdown", exportRun.ContentType);
        Assert.Contains("Markdown export", exportRun.Summary, StringComparison.Ordinal);

        var approval = await exportApprovalService.ApproveAsync(
            project.Id,
            new ApproveExportRequest(export.FileName, "Senior Engineer", "Ready for supervised onboarding."),
            CancellationToken.None);

        Assert.NotNull(approval);
        Assert.Equal(export.FileName, approval.FileName);
        Assert.Equal("Senior Engineer", approval.Reviewer);

        var approvals = await exportApprovalService.ListAsync(project.Id, CancellationToken.None);
        var listedApproval = Assert.Single(approvals);
        Assert.Equal(approval.Id, listedApproval.Id);
        Assert.Contains("Ready for supervised onboarding", listedApproval.Summary, StringComparison.Ordinal);
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
        Assert.Equal(1, workflowRow.ReviewHistoryCount);
        Assert.Equal("Approve", workflowRow.LatestReviewAction);
        Assert.NotNull(workflowRow.LatestReviewAt);
        Assert.True(workflowRow.RoadmapUsageCount > 0);
        Assert.True(workflowRow.IncludedInExport);
    }

    [Fact]
    public async Task ComplianceMatrixService_reports_compliant_and_open_evidence()
    {
        var repository = new InMemoryProjectRepository();
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(storageRoot);
        var auditLog = new JsonAuditLog(Path.Combine(storageRoot, "audit-log.json"));
        var parser = new PlainTextDocumentParser();
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [parser]);
        var extractionService = new KnowledgeExtractionService(repository, new HeuristicKnowledgeExtractor());
        var reviewService = new KnowledgeReviewService(repository);
        var exportApprovalService = new ExportApprovalService(repository, auditLog);
        var complianceService = new ComplianceMatrixService(repository, auditLog);

        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Compliance Project", "Compliance matrix test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("""
            CTU communication is unknown.

            Deployment workflow must be reviewed by senior engineers.
            """u8.ToArray());

        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand("compliance.md", "text/markdown", "Manual upload", content),
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

        var matrixBeforeApproval = await complianceService.GetMatrixAsync(project.Id, CancellationToken.None);

        Assert.NotNull(matrixBeforeApproval);
        var rowBeforeApproval = matrixBeforeApproval.Rows.Single(row => row.KnowledgeItemId == workflow.Id);
        Assert.False(rowBeforeApproval.ExportReady);
        Assert.Equal("OpenIssue", rowBeforeApproval.ComplianceStatus);
        Assert.Equal("Export wurde noch nicht explizit freigegeben.", rowBeforeApproval.Gap);

        await exportApprovalService.ApproveAsync(
            project.Id,
            new ApproveExportRequest("compliance-project-knowledge-transfer.md", "Senior Engineer", "Export evidence approved."),
            CancellationToken.None);

        var matrix = await complianceService.GetMatrixAsync(project.Id, CancellationToken.None);

        Assert.NotNull(matrix);
        Assert.True(matrix.TotalRows >= extraction.KnowledgeItems.Count);
        Assert.True(matrix.CompliantCount >= 1);
        Assert.True(matrix.OpenIssueCount >= 1);

        var workflowRow = matrix.Rows.Single(row => row.KnowledgeItemId == workflow.Id);
        Assert.Equal("compliance.md", workflowRow.Source);
        Assert.True(workflowRow.ExportReady);
        Assert.Equal("Compliant", workflowRow.ComplianceStatus);
        Assert.Equal("Nachweis vollstaendig.", workflowRow.Gap);

        Assert.Contains(matrix.Rows, row =>
            row.ComplianceStatus == "OpenIssue"
            && row.Gap.Contains("nicht freigegeben", StringComparison.Ordinal));
    }

    private static int CountFor(IReadOnlyCollection<KnowledgeReviewSummaryGroupResponse> groups, string name)
    {
        return groups.FirstOrDefault(group => group.Name == name)?.Count ?? 0;
    }
}
