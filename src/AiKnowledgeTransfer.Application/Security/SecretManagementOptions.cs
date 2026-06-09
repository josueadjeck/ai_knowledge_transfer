namespace AiKnowledgeTransfer.Application.Security;

public sealed record SecretManagementOptions(
    string Mode,
    string? Provider,
    string? RotationOwner)
{
    public static SecretManagementOptions Default { get; } = new("Environment", null, null);

    public static SecretManagementOptions FromEnvironment()
    {
        var mode = Environment.GetEnvironmentVariable("AKT_SECRET_STORE_MODE");
        var provider = Environment.GetEnvironmentVariable("AKT_SECRET_STORE_PROVIDER");
        var rotationOwner = Environment.GetEnvironmentVariable("AKT_SECRET_ROTATION_OWNER");

        return new SecretManagementOptions(
            string.IsNullOrWhiteSpace(mode) ? "Environment" : mode,
            provider,
            rotationOwner);
    }

    public bool IsSecretStore => Mode.Equals("SecretStore", StringComparison.OrdinalIgnoreCase);

    public bool IsEnvironment => Mode.Equals("Environment", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured => IsEnvironment
        || (IsSecretStore
            && !string.IsNullOrWhiteSpace(Provider)
            && !string.IsNullOrWhiteSpace(RotationOwner));
}
