namespace AiKnowledgeTransfer.Infrastructure.Audit;

using System.Text.Json;
using AiKnowledgeTransfer.Application.Abstractions;

public sealed class JsonAuditLog : IAuditLog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public JsonAuditLog(string filePath)
    {
        _filePath = filePath;
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var events = LoadEvents().ToList();
            events.Add(auditEvent);
            await SaveEventsAsync(events, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<AuditEvent>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return LoadEvents()
                .Where(auditEvent => projectId is null || auditEvent.ProjectId == projectId)
                .OrderByDescending(auditEvent => auditEvent.OccurredAt)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private IReadOnlyCollection<AuditEvent> LoadEvents()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        using var fileStream = File.OpenRead(_filePath);
        var snapshot = JsonSerializer.Deserialize<AuditLogSnapshot>(fileStream, JsonOptions);
        return snapshot?.Events ?? [];
    }

    private async Task SaveEventsAsync(IReadOnlyCollection<AuditEvent> events, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_filePath}.tmp";
        await using (var fileStream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(fileStream, new AuditLogSnapshot(events), JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private sealed record AuditLogSnapshot(IReadOnlyCollection<AuditEvent> Events);
}
