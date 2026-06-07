namespace AiKnowledgeTransfer.Contracts.Diagnostics;

public sealed record HealthComponentResponse(
    string Name,
    string Status,
    string Detail,
    IReadOnlyDictionary<string, string> Metadata);
