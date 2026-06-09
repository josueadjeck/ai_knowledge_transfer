namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record RuntimeMetricsResponse(
    string Service,
    DateTimeOffset CapturedAt,
    long UptimeSeconds,
    int ProcessId,
    string MachineName,
    long WorkingSetBytes,
    long GcMemoryBytes,
    int ThreadCount,
    int ProcessorCount,
    string RuntimeVersion);
