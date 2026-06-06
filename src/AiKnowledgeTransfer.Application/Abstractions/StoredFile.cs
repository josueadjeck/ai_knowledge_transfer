namespace AiKnowledgeTransfer.Application.Abstractions;

public sealed record StoredFile(
    string FileName,
    string ContentType,
    long SizeInBytes,
    string StoragePath);
