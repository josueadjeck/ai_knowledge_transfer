namespace AiKnowledgeTransfer.Contracts.Exports;

public sealed record ExportHistoryResponse(
    Guid Id,
    Guid ProjectId,
    string FileName,
    string ContentType,
    string Summary,
    DateTimeOffset ExportedAt);
