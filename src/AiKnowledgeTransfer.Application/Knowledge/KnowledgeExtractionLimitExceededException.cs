namespace AiKnowledgeTransfer.Application.Knowledge;

public sealed class KnowledgeExtractionLimitExceededException : InvalidOperationException
{
    public KnowledgeExtractionLimitExceededException(
        Guid documentId,
        string limitName,
        int actualValue,
        int configuredLimit)
        : base($"Document '{documentId}' has {actualValue} {limitName} for knowledge extraction. The configured extraction limit is {configuredLimit} {limitName}.")
    {
        DocumentId = documentId;
        LimitName = limitName;
        ActualValue = actualValue;
        ConfiguredLimit = configuredLimit;
    }

    public Guid DocumentId { get; }

    public string LimitName { get; }

    public int ActualValue { get; }

    public int ConfiguredLimit { get; }
}
