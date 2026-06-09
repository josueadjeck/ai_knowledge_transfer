namespace AiKnowledgeTransfer.Infrastructure;

using AiKnowledgeTransfer.Application.Security;

public sealed class RuntimeSecretResolver(SecretManagementOptions options)
{
    public string? GetSecret(string name)
    {
        if (options.IsSecretStore && options.IsMountedFilesProvider && !string.IsNullOrWhiteSpace(options.MountPath))
        {
            var filePath = Path.GetFullPath(Path.Combine(options.MountPath, name));
            var mountRoot = Path.GetFullPath(options.MountPath);
            if (!filePath.StartsWith(mountRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Secret path escapes the configured mount directory.");
            }

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath).Trim();
            }
        }

        return Environment.GetEnvironmentVariable(name);
    }
}
