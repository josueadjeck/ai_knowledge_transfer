namespace AiKnowledgeTransfer.Contracts.Operations;

public sealed record MonitoringSummaryResponse(
    string Service,
    DateTimeOffset CapturedAt,
    string Status,
    string HealthStatus,
    string ReleaseReadinessStatus,
    long UptimeSeconds,
    long WorkingSetBytes,
    long GcMemoryBytes,
    int ThreadCount,
    int ErrorCount,
    int WarningCount,
    int ManualReviewCount,
    IReadOnlyCollection<MonitoringSignalResponse> Signals);
