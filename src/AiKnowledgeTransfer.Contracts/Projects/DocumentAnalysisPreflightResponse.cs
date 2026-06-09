namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentAnalysisPreflightResponse(
    Guid DocumentId,
    string FileName,
    string Status,
    string WorkloadClass,
    long SizeInBytes,
    int AnalysisCharacterLimit,
    int ExtractionCharacterLimit,
    decimal AnalysisLimitUtilizationPercent,
    decimal ExtractionLimitUtilizationPercent,
    bool ParserAvailable,
    string ParserName,
    IReadOnlyCollection<string> Recommendations);
