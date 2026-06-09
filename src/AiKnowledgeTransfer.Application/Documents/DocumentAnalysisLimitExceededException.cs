namespace AiKnowledgeTransfer.Application.Documents;

public sealed class DocumentAnalysisLimitExceededException : InvalidOperationException
{
    public DocumentAnalysisLimitExceededException(
        string fileName,
        string limitName,
        int actualValue,
        int configuredLimit)
        : base($"Document '{fileName}' produced {actualValue} {limitName}. The configured analysis limit is {configuredLimit} {limitName}.")
    {
        FileName = fileName;
        LimitName = limitName;
        ActualValue = actualValue;
        ConfiguredLimit = configuredLimit;
    }

    public string FileName { get; }

    public string LimitName { get; }

    public int ActualValue { get; }

    public int ConfiguredLimit { get; }
}
