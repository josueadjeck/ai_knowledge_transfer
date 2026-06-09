namespace AiKnowledgeTransfer.Application.Security;

public sealed record SecretManagementOptions(
    string Mode,
    string? Provider,
    string? RotationOwner,
    string? MountPath = null)
{
    public static SecretManagementOptions Default { get; } = new("Environment", null, null);

    public static SecretManagementOptions FromEnvironment()
    {
        var mode = Environment.GetEnvironmentVariable("AKT_SECRET_STORE_MODE");
        var provider = Environment.GetEnvironmentVariable("AKT_SECRET_STORE_PROVIDER");
        var rotationOwner = Environment.GetEnvironmentVariable("AKT_SECRET_ROTATION_OWNER");
        var mountPath = Environment.GetEnvironmentVariable("AKT_SECRET_MOUNT_PATH");

        return new SecretManagementOptions(
            string.IsNullOrWhiteSpace(mode) ? "Environment" : mode,
            provider,
            rotationOwner,
            mountPath);
    }

    public bool IsSecretStore => Mode.Equals("SecretStore", StringComparison.OrdinalIgnoreCase);

    public bool IsEnvironment => Mode.Equals("Environment", StringComparison.OrdinalIgnoreCase);

    public bool IsMountedFilesProvider => Provider?.Equals("MountedFiles", StringComparison.OrdinalIgnoreCase) == true;

    public bool IsConfigured => IsEnvironment
        || (IsSecretStore
            && !string.IsNullOrWhiteSpace(Provider)
            && !string.IsNullOrWhiteSpace(RotationOwner)
            && (!IsMountedFilesProvider || !string.IsNullOrWhiteSpace(MountPath)));
}
