namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    string Source,
    long SizeInBytes,
    int VersionNumber,
    string StoragePath,
    string Status,
    int ChunkCount,
    DateTimeOffset UploadedAt);
