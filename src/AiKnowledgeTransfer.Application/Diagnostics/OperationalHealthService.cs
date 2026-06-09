using AiKnowledgeTransfer.Contracts.Diagnostics;

namespace AiKnowledgeTransfer.Application.Diagnostics;

public sealed class OperationalHealthService
{
    private readonly OperationalHealthOptions _options;

    public OperationalHealthService(OperationalHealthOptions options)
    {
        _options = options;
    }

    public HealthResponse GetStatus(string serviceName)
    {
        var components = new[]
        {
            GetStorageComponent(),
            GetPersistenceComponent(),
            GetProjectStoreComponent(),
            GetAuditLogComponent(),
            GetAuthComponent(),
            GetTenancyComponent(),
            GetAiProviderComponent(),
            GetAnalysisLimitsComponent(),
            GetExtractionLimitsComponent()
        };

        var status = components.Any(component => component.Status == "error")
            ? "degraded"
            : "ok";

        return new HealthResponse(status, serviceName, DateTimeOffset.UtcNow, components);
    }

    private HealthComponentResponse GetStorageComponent()
    {
        try
        {
            Directory.CreateDirectory(_options.AppDataPath);
            Directory.CreateDirectory(_options.UploadStoragePath);

            return new HealthComponentResponse(
                "storage",
                "ok",
                "Local storage is reachable.",
                new Dictionary<string, string>
                {
                    ["appDataPath"] = _options.AppDataPath,
                    ["uploadStoragePath"] = _options.UploadStoragePath
                });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new HealthComponentResponse(
                "storage",
                "error",
                exception.Message,
                new Dictionary<string, string>
                {
                    ["appDataPath"] = _options.AppDataPath,
                    ["uploadStoragePath"] = _options.UploadStoragePath
                });
        }
    }

    private HealthComponentResponse GetProjectStoreComponent()
    {
        if (_options.PersistenceProvider.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthComponentResponse(
                "projectStore",
                "database",
                "Project data is handled by the configured database provider.",
                new Dictionary<string, string>
                {
                    ["persistenceProvider"] = _options.PersistenceProvider
                });
        }

        return GetFileComponent("projectStore", _options.ProjectStorePath);
    }

    private HealthComponentResponse GetAuditLogComponent()
    {
        if (_options.PersistenceProvider.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            return new HealthComponentResponse(
                "auditLog",
                "database",
                "Audit events are handled by the configured database provider.",
                new Dictionary<string, string>
                {
                    ["persistenceProvider"] = _options.PersistenceProvider
                });
        }

        return GetFileComponent("auditLog", _options.AuditLogPath);
    }

    private HealthComponentResponse GetPersistenceComponent()
    {
        var isDatabase = _options.PersistenceProvider.Equals("Database", StringComparison.OrdinalIgnoreCase);
        var hasConnectionString = !string.IsNullOrWhiteSpace(_options.DatabaseConnectionString);
        var status = isDatabase && !hasConnectionString ? "error" : "ok";
        var detail = isDatabase
            ? "Database persistence is active."
            : "JSON persistence is active.";

        return new HealthComponentResponse(
            "persistence",
            status,
            detail,
            new Dictionary<string, string>
            {
                ["provider"] = _options.PersistenceProvider,
                ["databaseProvider"] = _options.DatabaseProvider ?? string.Empty,
                ["connectionStringConfigured"] = hasConnectionString.ToString(),
                ["projectStorePath"] = _options.ProjectStorePath,
                ["auditLogPath"] = _options.AuditLogPath
            });
    }

    private static HealthComponentResponse GetFileComponent(string name, string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        return new HealthComponentResponse(
            name,
            Directory.Exists(directory) ? "ok" : "error",
            File.Exists(path) ? "File exists." : "File will be created on first write.",
            new Dictionary<string, string>
            {
                ["path"] = path,
                ["exists"] = File.Exists(path).ToString()
            });
    }

    private HealthComponentResponse GetAiProviderComponent()
    {
        var status = _options.AiApiKeyConfigured ? "ok" : "fallback";
        var detail = _options.AiApiKeyConfigured
            ? $"{_options.AiProvider} provider is configured."
            : "OpenAI API key is not configured; local heuristic extractor is active.";

        return new HealthComponentResponse(
            "aiProvider",
            status,
            detail,
            new Dictionary<string, string>
            {
                ["provider"] = _options.AiProvider,
                ["model"] = _options.AiModel,
                ["baseUrl"] = _options.AiBaseUrl,
                ["apiKeyConfigured"] = _options.AiApiKeyConfigured.ToString()
            });
    }

    private HealthComponentResponse GetAuthComponent()
    {
        var isOidc = _options.AuthMode.Equals("Oidc", StringComparison.OrdinalIgnoreCase);
        var isConfigured = !isOidc
            || (!string.IsNullOrWhiteSpace(_options.AuthAuthority)
                && !string.IsNullOrWhiteSpace(_options.AuthClientId));
        var status = isConfigured ? "ok" : "error";
        var detail = isOidc
            ? "OIDC authentication mode is selected."
            : "Demo authentication mode is active; roles are resolved from fallback or demo selector.";

        return new HealthComponentResponse(
            "authentication",
            status,
            detail,
            new Dictionary<string, string>
            {
                ["mode"] = _options.AuthMode,
                ["authorityConfigured"] = (!string.IsNullOrWhiteSpace(_options.AuthAuthority)).ToString(),
                ["clientIdConfigured"] = (!string.IsNullOrWhiteSpace(_options.AuthClientId)).ToString(),
                ["roleClaimType"] = _options.AuthRoleClaimType
            });
    }

    private HealthComponentResponse GetAnalysisLimitsComponent()
    {
        return new HealthComponentResponse(
            "analysisLimits",
            "ok",
            "Document analysis guardrails are configured.",
            new Dictionary<string, string>
            {
                ["maxChunksPerDocument"] = _options.MaxAnalysisChunksPerDocument.ToString(),
                ["maxExtractedCharacters"] = _options.MaxAnalysisExtractedCharacters.ToString()
            });
    }

    private HealthComponentResponse GetTenancyComponent()
    {
        var isSingleTenant = _options.TenancyMode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase);
        var isMultiTenant = _options.TenancyMode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);
        var isConfigured = (isSingleTenant || isMultiTenant)
            && !string.IsNullOrWhiteSpace(_options.TenantClaimType)
            && !string.IsNullOrWhiteSpace(_options.DefaultTenantId);

        return new HealthComponentResponse(
            "tenancy",
            isConfigured ? "ok" : "error",
            isMultiTenant
                ? "Multi-tenant mode is configured."
                : isSingleTenant
                    ? "Single-tenant mode is active."
                    : "Tenancy mode is invalid.",
            new Dictionary<string, string>
            {
                ["mode"] = _options.TenancyMode,
                ["tenantClaimType"] = _options.TenantClaimType,
                ["defaultTenantId"] = _options.DefaultTenantId,
                ["multiTenantEnabled"] = isMultiTenant.ToString()
            });
    }

    private HealthComponentResponse GetExtractionLimitsComponent()
    {
        return new HealthComponentResponse(
            "extractionLimits",
            "ok",
            "Knowledge extraction guardrails are configured.",
            new Dictionary<string, string>
            {
                ["maxChunksPerExtraction"] = _options.MaxExtractionChunks.ToString(),
                ["maxChunkCharacters"] = _options.MaxExtractionChunkCharacters.ToString()
            });
    }
}
