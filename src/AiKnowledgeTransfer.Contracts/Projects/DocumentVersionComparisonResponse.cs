namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentVersionComparisonResponse(
    Guid ProjectId,
    Guid BaseDocumentId,
    Guid TargetDocumentId,
    string FileName,
    int BaseVersionNumber,
    int TargetVersionNumber,
    int BaseChunkCount,
    int TargetChunkCount,
    int AddedCount,
    int RemovedCount,
    int ChangedCount,
    int UnchangedCount,
    IReadOnlyCollection<DocumentVersionComparisonRowResponse> Rows);
