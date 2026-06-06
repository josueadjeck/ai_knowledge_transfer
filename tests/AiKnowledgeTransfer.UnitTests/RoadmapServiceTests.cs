namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Infrastructure.Persistence;

public sealed class RoadmapServiceTests
{
    [Fact]
    public async Task GenerateAsync_creates_requested_number_of_weeks()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectService(repository);
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
