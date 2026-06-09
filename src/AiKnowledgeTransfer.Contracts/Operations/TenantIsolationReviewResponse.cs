namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record TenantIsolationReviewResponse(
    string Status,
    DateTimeOffset GeneratedAt,
    string TenancyMode,
    string TenantClaimType,
    string DefaultTenantId,
    int RequiredAreaCount,
    int ManualAreaCount,
    IReadOnlyCollection<TenantIsolationAreaResponse> Areas,
    IReadOnlyCollection<string> Notes);
