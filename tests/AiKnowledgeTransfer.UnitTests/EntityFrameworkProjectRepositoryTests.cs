using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Infrastructure.Parsing;
using AiKnowledgeTransfer.Infrastructure.Persistence.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class EntityFrameworkProjectRepositoryTests
{
    [Fact]
    public async Task Repository_persists_and_loads_project_aggregate_with_documents()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new KnowledgeTransferDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid projectId;
        await using (var writeContext = new KnowledgeTransferDbContext(options))
        {
            var repository = new EntityFrameworkProjectRepository(writeContext);
            var storage = new Infrastructure.Storage.LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-ef-tests", Guid.NewGuid().ToString("N")));
            var service = new Application.Projects.ProjectService(repository, storage);

            var project = await service.CreateAsync(
                new CreateProjectRequest("SQLite Project", "Relational persistence test", "Engineering"),
                CancellationToken.None);
            projectId = project.Id;

            await using var content = new MemoryStream("SQLite backed document"u8.ToArray());
            await service.UploadDocumentAsync(
                project.Id,
                new Application.Projects.UploadDocumentCommand("sqlite.md", "text/markdown", "Test", content),
                CancellationToken.None);
        }

        await using (var readContext = new KnowledgeTransferDbContext(options))
        {
            var repository = new EntityFrameworkProjectRepository(readContext);
            var project = await repository.GetAsync(projectId, CancellationToken.None);

            Assert.NotNull(project);
            Assert.Equal("SQLite Project", project.Name);
            Assert.Single(project.Documents);
            Assert.Equal("sqlite.md", project.Documents.Single().FileName);
        }
    }

    [Fact]
    public async Task Repository_persists_and_loads_knowledge_review_history()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new KnowledgeTransferDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid projectId;
        Guid itemId;
        await using (var writeContext = new KnowledgeTransferDbContext(options))
        {
            var repository = new EntityFrameworkProjectRepository(writeContext);
            var storage = new Infrastructure.Storage.LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-ef-tests", Guid.NewGuid().ToString("N")));
            var service = new Application.Projects.ProjectService(repository, storage);
            var reviewService = new KnowledgeReviewService(repository);

            var project = await service.CreateAsync(
                new CreateProjectRequest("SQLite Review Project", "Review history persistence test", "Engineering"),
                CancellationToken.None);
            var details = await service.GetAsync(project.Id, CancellationToken.None);

            Assert.NotNull(details);
            projectId = project.Id;
            itemId = details.KnowledgeItems.First().Id;

            await reviewService.ApproveAsync(
                project.Id,
                itemId,
                new ReviewKnowledgeItemRequest("Senior Engineer", "SQLite persisted review."),
                CancellationToken.None);
        }

        await using (var readContext = new KnowledgeTransferDbContext(options))
        {
            var repository = new EntityFrameworkProjectRepository(readContext);
            var project = await repository.GetAsync(projectId, CancellationToken.None);

            Assert.NotNull(project);
            var item = project.KnowledgeItems.Single(candidate => candidate.Id == itemId);
            var history = Assert.Single(item.ReviewHistory);
            Assert.Equal("Approve", history.Action);
            Assert.Equal("Senior Engineer", history.Reviewer);
            Assert.Equal("Verified", history.QualityStatus);
        }
    }

    [Fact]
    public async Task Repository_persists_and_loads_document_chunk_quality_metadata()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new KnowledgeTransferDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid projectId;
        await using (var writeContext = new KnowledgeTransferDbContext(options))
        {
            var repository = new EntityFrameworkProjectRepository(writeContext);
            var storage = new Infrastructure.Storage.LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-ef-tests", Guid.NewGuid().ToString("N")));
            var projectService = new Application.Projects.ProjectService(repository, storage);
            var analysisService = new DocumentAnalysisService(repository, storage, [new PlainTextDocumentParser()]);

            var project = await projectService.CreateAsync(
                new CreateProjectRequest("SQLite Chunk Project", "Chunk quality persistence test", "Engineering"),
                CancellationToken.None);
            projectId = project.Id;

            await using var content = new MemoryStream("This paragraph has enough technical context to be reviewed as usable source text."u8.ToArray());
            var upload = await projectService.UploadDocumentAsync(
                project.Id,
                new Application.Projects.UploadDocumentCommand("quality.md", "text/markdown", "Test", content),
                CancellationToken.None);

            Assert.NotNull(upload);
            await analysisService.AnalyzeAsync(project.Id, upload.Document.Id, CancellationToken.None);
        }

        await using (var readContext = new KnowledgeTransferDbContext(options))
        {
            var repository = new EntityFrameworkProjectRepository(readContext);
            var project = await repository.GetAsync(projectId, CancellationToken.None);

            Assert.NotNull(project);
            var chunk = Assert.Single(project.Documents.Single().Chunks);
            Assert.Equal("UsableText", chunk.QualityStatus);
            Assert.Contains("Plain text body", chunk.SourceReference, StringComparison.Ordinal);
        }
    }
}
