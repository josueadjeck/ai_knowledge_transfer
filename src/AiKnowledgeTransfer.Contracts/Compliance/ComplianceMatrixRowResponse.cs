namespace AiKnowledgeTransfer.Contracts.Compliance;

public sealed record ComplianceMatrixRowResponse(
    Guid? DocumentId,
    string Source,
    Guid? KnowledgeItemId,
    string KnowledgeTitle,
    string EvidenceType,
    string ReviewStatus,
    string QualityStatus,
    int ReviewHistoryCount,
    bool ExportReady,
    string ComplianceStatus,
    string Gap);
