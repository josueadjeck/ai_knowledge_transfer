namespace AiKnowledgeTransfer.Application.Diagnostics;

public sealed record OperationalHealthOptions(
    string AppDataPath,
    string UploadStoragePath,
    string ProjectStorePath,
    string AuditLogPath,
    string PersistenceProvider,
    string? DatabaseProvider,
    string? DatabaseConnectionString,
    string AiProvider,
    string AiModel,
    string AiBaseUrl,
    bool AiApiKeyConfigured,
    int MaxAnalysisChunksPerDocument,
    int MaxAnalysisExtractedCharacters,
    string AuthMode,
    string? AuthAuthority,
    string? AuthClientId,
    string AuthRoleClaimType);
