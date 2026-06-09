namespace AiKnowledgeTransfer.Application.Operations;

using System.Diagnostics;
using AiKnowledgeTransfer.Contracts.Operations;

public sealed class RuntimeMetricsService
{
    public RuntimeMetricsResponse GetSnapshot(string serviceName)
    {
        using var process = Process.GetCurrentProcess();
        var capturedAt = DateTimeOffset.UtcNow;
        var startedAt = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        var uptime = capturedAt - startedAt;

        return new RuntimeMetricsResponse(
            serviceName,
            capturedAt,
            Math.Max(0, (long)uptime.TotalSeconds),
            process.Id,
            Environment.MachineName,
            process.WorkingSet64,
            GC.GetTotalMemory(forceFullCollection: false),
            process.Threads.Count,
            Environment.ProcessorCount,
            Environment.Version.ToString());
    }
}
