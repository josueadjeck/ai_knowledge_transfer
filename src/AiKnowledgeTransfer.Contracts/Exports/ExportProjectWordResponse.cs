namespace AiKnowledgeTransfer.Contracts.Exports;

public sealed record ExportProjectWordResponse(
    Guid ProjectId,
    string FileName,
    string ContentType,
    byte[] Document,
    DateTimeOffset GeneratedAt);
