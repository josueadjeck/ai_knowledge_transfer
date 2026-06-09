namespace AiKnowledgeTransfer.Application.Operations;

using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Contracts.Operations;

public sealed class MonitoringSummaryService(
    OperationalHealthService health,
    ReleaseReadinessService releaseReadiness,
    RuntimeMetricsService runtimeMetrics)
{
    public MonitoringSummaryResponse GetSummary(string serviceName)
    {
        var healthStatus = health.GetStatus(serviceName);
        var readiness = releaseReadiness.GetStatus(serviceName);
        var metrics = runtimeMetrics.GetSnapshot(serviceName);

        var healthErrors = healthStatus.Components
            .Where(component => component.Status.Equals("error", StringComparison.OrdinalIgnoreCase))
            .Select(component => new MonitoringSignalResponse(
                component.Name,
                "Error",
                component.Detail));

        var readinessSignals = readiness.Checks
            .Where(check => !check.Status.Equals("Pass", StringComparison.OrdinalIgnoreCase))
            .Select(check => new MonitoringSignalResponse(
                check.Name,
                check.Status,
                check.Detail));

        var signals = healthErrors
            .Concat(readinessSignals)
            .OrderBy(signal => SignalPriority(signal.Status))
            .ThenBy(signal => signal.Name, StringComparer.Ordinal)
            .ToArray();

        var errorCount = signals.Count(signal =>
            signal.Status.Equals("Error", StringComparison.OrdinalIgnoreCase)
            || signal.Status.Equals("Fail", StringComparison.OrdinalIgnoreCase));
        var warningCount = signals.Count(signal =>
            signal.Status.Equals("Warning", StringComparison.OrdinalIgnoreCase));
        var manualReviewCount = signals.Count(signal =>
            signal.Status.Equals("Manual", StringComparison.OrdinalIgnoreCase));

        var status = errorCount > 0 || healthStatus.Status.Equals("degraded", StringComparison.OrdinalIgnoreCase)
            ? "degraded"
            : warningCount > 0 || manualReviewCount > 0 || readiness.Status.Equals("NeedsReview", StringComparison.OrdinalIgnoreCase)
                ? "needsReview"
                : "ok";

        return new MonitoringSummaryResponse(
            serviceName,
            metrics.CapturedAt,
            status,
            healthStatus.Status,
            readiness.Status,
            metrics.UptimeSeconds,
            metrics.WorkingSetBytes,
            metrics.GcMemoryBytes,
            metrics.ThreadCount,
            errorCount,
            warningCount,
            manualReviewCount,
            signals);
    }

    private static int SignalPriority(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "ERROR" or "FAIL" => 0,
            "WARNING" => 1,
            "MANUAL" => 2,
            _ => 3
        };
    }
}
