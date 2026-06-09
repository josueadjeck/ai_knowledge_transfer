namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record MonitoringSignalResponse(
    string Name,
    string Status,
    string Detail);
