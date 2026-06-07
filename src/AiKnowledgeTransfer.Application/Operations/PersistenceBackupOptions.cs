namespace AiKnowledgeTransfer.Application.Operations;

public sealed record PersistenceBackupOptions(
    string AppDataPath,
    string UploadStoragePath,
    string ProjectStorePath,
    string AuditLogPath,
    string BackupPath);
