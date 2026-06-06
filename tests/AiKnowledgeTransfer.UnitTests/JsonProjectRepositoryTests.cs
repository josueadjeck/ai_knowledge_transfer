namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Infrastructure.Persistence;
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
}
