namespace AiKnowledgeTransfer.Infrastructure.Storage;

using AiKnowledgeTransfer.Application.Abstractions;

public sealed class LocalFileStorage(string rootPath) : IFileStorage
{
    public async Task<StoredFile> SaveAsync(
        Guid projectId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must not be empty.", nameof(fileName));
        }

        var safeFileName = Path.GetFileName(fileName);
        var projectDirectory = Path.Combine(rootPath, projectId.ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        var storedFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{safeFileName}";
        var absolutePath = Path.Combine(projectDirectory, storedFileName);

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        var fileInfo = new FileInfo(absolutePath);
        var relativePath = Path.GetRelativePath(rootPath, absolutePath);

        return new StoredFile(
            safeFileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            fileInfo.Length,
            relativePath);
    }
}
