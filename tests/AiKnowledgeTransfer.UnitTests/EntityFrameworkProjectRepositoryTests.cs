using AiKnowledgeTransfer.Contracts.Projects;
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
}
