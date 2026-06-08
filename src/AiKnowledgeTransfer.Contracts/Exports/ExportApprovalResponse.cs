namespace AiKnowledgeTransfer.Contracts.Exports;

public sealed record ExportApprovalResponse(
    Guid Id,
    Guid ProjectId,
    string FileName,
    string Reviewer,
    string Summary,
    DateTimeOffset ApprovedAt);
