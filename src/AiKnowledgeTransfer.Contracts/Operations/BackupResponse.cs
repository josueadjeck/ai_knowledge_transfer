namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record BackupResponse(
    string FileName,
    string Path,
    DateTimeOffset CreatedAt,
    long SizeInBytes);
