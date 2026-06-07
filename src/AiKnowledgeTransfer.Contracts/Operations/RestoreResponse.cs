namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record RestoreResponse(
    string FileName,
    DateTimeOffset RestoredAt,
    IReadOnlyCollection<string> RestoredEntries);
