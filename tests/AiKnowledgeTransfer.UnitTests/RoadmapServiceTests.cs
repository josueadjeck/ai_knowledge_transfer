namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
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
