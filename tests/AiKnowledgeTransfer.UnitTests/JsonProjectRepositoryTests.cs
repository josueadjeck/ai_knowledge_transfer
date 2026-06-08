namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Parsing;
using AiKnowledgeTransfer.Infrastructure.Storage;

public sealed class JsonProjectRepositoryTests
{
    [Fact]
    public async Task SaveChangesAsync_persists_projects_for_new_repository_instance()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storePath = Path.Combine(storageRoot, "projects.json");
        var repository = new JsonProjectRepository(storePath);
        var storage = new LocalFileStorage(Path.Combine(storageRoot, "uploads"));
        var projectService = new ProjectService(repository, storage);

        var created = await projectService.CreateAsync(
            new CreateProjectRequest("Persistent Project", "Persistence test", "Engineering"),
            CancellationToken.None);

        await projectService.RegisterDocumentAsync(
            created.Id,
            new RegisterDocumentRequest("architecture.md", "text/markdown", "Manual registration", 42),
            CancellationToken.None);

        var reloadedRepository = new JsonProjectRepository(storePath);
        var reloaded = await reloadedRepository.GetAsync(created.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("Persistent Project", reloaded.Name);
        Assert.Single(reloaded.Documents);
        Assert.True(File.Exists(storePath));
    }

    [Fact]
    public async Task SaveChangesAsync_persists_knowledge_review_history()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storePath = Path.Combine(storageRoot, "projects.json");
        var repository = new JsonProjectRepository(storePath);
        var storage = new LocalFileStorage(Path.Combine(storageRoot, "uploads"));
        var projectService = new ProjectService(repository, storage);
        var reviewService = new KnowledgeReviewService(repository);

        var created = await projectService.CreateAsync(
            new CreateProjectRequest("Review History Project", "Persistence test", "Engineering"),
            CancellationToken.None);
        var details = await projectService.GetAsync(created.Id, CancellationToken.None);

        Assert.NotNull(details);
        var itemId = details.KnowledgeItems.First().Id;
        await reviewService.ApproveAsync(
            created.Id,
            itemId,
            new ReviewKnowledgeItemRequest("Senior Engineer", "Persisted review."),
            CancellationToken.None);

        var reloadedRepository = new JsonProjectRepository(storePath);
        var reloaded = await reloadedRepository.GetAsync(created.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        var item = reloaded.KnowledgeItems.Single(candidate => candidate.Id == itemId);
        var history = Assert.Single(item.ReviewHistory);
        Assert.Equal("Approve", history.Action);
        Assert.Equal("Senior Engineer", history.Reviewer);
        Assert.Equal("Verified", history.QualityStatus);
    }

    [Fact]
    public async Task SaveChangesAsync_persists_document_chunk_quality_metadata()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N"));
        var storePath = Path.Combine(storageRoot, "projects.json");
        var repository = new JsonProjectRepository(storePath);
        var storage = new LocalFileStorage(Path.Combine(storageRoot, "uploads"));
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(repository, storage, [new PlainTextDocumentParser()]);

        var created = await projectService.CreateAsync(
            new CreateProjectRequest("Chunk Quality Project", "Persistence test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("This paragraph has enough technical context to be reviewed as usable source text."u8.ToArray());
        var upload = await projectService.UploadDocumentAsync(
            created.Id,
            new UploadDocumentCommand("quality.md", "text/markdown", "Manual upload", content),
            CancellationToken.None);

        Assert.NotNull(upload);
        await analysisService.AnalyzeAsync(created.Id, upload.Document.Id, CancellationToken.None);

        var reloadedRepository = new JsonProjectRepository(storePath);
        var reloaded = await reloadedRepository.GetAsync(created.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        var chunk = Assert.Single(reloaded.Documents.Single().Chunks);
        Assert.Equal("UsableText", chunk.QualityStatus);
        Assert.Contains("Plain text body", chunk.SourceReference, StringComparison.Ordinal);
    }
}
