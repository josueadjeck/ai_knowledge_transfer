namespace AiKnowledgeTransfer.Contracts.Compliance;

public sealed record ComplianceMatrixResponse(
    Guid ProjectId,
    string ProjectName,
    DateTimeOffset GeneratedAt,
    int TotalRows,
    int CompliantCount,
    int OpenIssueCount,
    IReadOnlyCollection<ComplianceMatrixRowResponse> Rows);
