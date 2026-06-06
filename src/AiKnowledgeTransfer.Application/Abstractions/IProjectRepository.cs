namespace AiKnowledgeTransfer.Application.Abstractions;

using AiKnowledgeTransfer.Domain.Projects;

public interface IProjectRepository
{
    Task<IReadOnlyCollection<KnowledgeProject>> ListAsync(CancellationToken cancellationToken);

    Task<KnowledgeProject?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(KnowledgeProject project, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
