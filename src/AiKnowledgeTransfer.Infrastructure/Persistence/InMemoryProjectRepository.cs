namespace AiKnowledgeTransfer.Infrastructure.Persistence;

using System.Collections.Concurrent;
using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Projects;

public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly ConcurrentDictionary<Guid, KnowledgeProject> _projects = new();

    public Task<IReadOnlyCollection<KnowledgeProject>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<KnowledgeProject> projects = _projects.Values
            .OrderByDescending(project => project.CreatedAt)
            .ToArray();

        return Task.FromResult(projects);
    }

    public Task<KnowledgeProject?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _projects.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    public Task AddAsync(KnowledgeProject project, CancellationToken cancellationToken)
    {
        _projects.TryAdd(project.Id, project);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
