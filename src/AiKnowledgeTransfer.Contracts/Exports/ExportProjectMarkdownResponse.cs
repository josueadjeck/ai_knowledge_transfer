namespace AiKnowledgeTransfer.Contracts.Exports;

public sealed record ExportProjectMarkdownResponse(
    Guid ProjectId,
    string FileName,
    string ContentType,
    string Markdown,
    DateTimeOffset GeneratedAt);
