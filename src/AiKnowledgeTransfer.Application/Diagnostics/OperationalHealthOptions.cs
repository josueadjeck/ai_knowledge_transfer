namespace AiKnowledgeTransfer.Application.Diagnostics;

public sealed record OperationalHealthOptions(
    string AppDataPath,
    string UploadStoragePath,
    string ProjectStorePath,
    string AuditLogPath,
    string AiProvider,
    string AiModel,
    string AiBaseUrl,
    bool AiApiKeyConfigured);
