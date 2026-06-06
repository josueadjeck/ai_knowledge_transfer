namespace AiKnowledgeTransfer.Contracts.Traceability;

public sealed record TraceabilityMatrixResponse(
    Guid ProjectId,
    string ProjectName,
    DateTimeOffset GeneratedAt,
    IReadOnlyCollection<TraceabilityRowResponse> Rows);
