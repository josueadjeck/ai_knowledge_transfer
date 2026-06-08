namespace AiKnowledgeTransfer.Contracts.Exports;

public sealed record ApproveExportRequest(
    string FileName,
    string Reviewer,
    string Comment);
