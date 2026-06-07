namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record BackupPreviewResponse(
    string FileName,
    DateTimeOffset? CreatedAt,
    string Format,
    int EntryCount,
    long SizeInBytes,
    bool ContainsProjects,
    bool ContainsAuditLog,
    int UploadFileCount,
    IReadOnlyCollection<string> Entries,
    IReadOnlyCollection<string> Warnings);
