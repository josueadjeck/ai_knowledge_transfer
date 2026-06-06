namespace AiKnowledgeTransfer.Application.Abstractions;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        Guid projectId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);
}
