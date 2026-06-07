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
            GetProjectStoreComponent(),
            GetAuditLogComponent(),
            GetAiProviderComponent()
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
        return GetFileComponent("projectStore", _options.ProjectStorePath);
    }

    private HealthComponentResponse GetAuditLogComponent()
    {
        return GetFileComponent("auditLog", _options.AuditLogPath);
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
}
