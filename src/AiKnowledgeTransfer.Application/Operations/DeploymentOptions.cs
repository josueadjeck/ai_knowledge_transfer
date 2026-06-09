namespace AiKnowledgeTransfer.Application.Operations;

public sealed record DeploymentOptions(
    string Strategy,
    string? ApprovalOwner)
{
    public static DeploymentOptions Default { get; } = new("SingleSlot", null);

    public static DeploymentOptions FromEnvironment()
    {
        var strategy = Environment.GetEnvironmentVariable("AKT_DEPLOYMENT_STRATEGY");
        var approvalOwner = Environment.GetEnvironmentVariable("AKT_DEPLOYMENT_APPROVAL_OWNER");

        return new DeploymentOptions(
            string.IsNullOrWhiteSpace(strategy) ? "SingleSlot" : strategy,
            approvalOwner);
    }

    public bool IsBlueGreen => Strategy.Equals("BlueGreen", StringComparison.OrdinalIgnoreCase);

    public bool IsSingleSlot => Strategy.Equals("SingleSlot", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured => (IsSingleSlot || IsBlueGreen)
        && (!IsBlueGreen || !string.IsNullOrWhiteSpace(ApprovalOwner));
}
