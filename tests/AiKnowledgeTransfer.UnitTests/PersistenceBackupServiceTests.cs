using AiKnowledgeTransfer.Application.Operations;
using Microsoft.Extensions.Logging;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class PersistenceBackupServiceTests
{
    [Fact]
    public async Task CreateAsync_includes_projects_audit_and_upload_files()
    {
        var options = CreateOptions();
        Directory.CreateDirectory(options.UploadStoragePath);
        await File.WriteAllTextAsync(options.ProjectStorePath, """{"projects":[]}""");
        await File.WriteAllTextAsync(options.AuditLogPath, """{"events":[]}""");
        await File.WriteAllTextAsync(Path.Combine(options.UploadStoragePath, "manual.md"), "# Manual");
        var service = new PersistenceBackupService(options);

        var backup = await service.CreateAsync(CancellationToken.None);

        Assert.True(File.Exists(backup.Path));
        Assert.Contains(service.List(), item => item.FileName == backup.FileName);
    }

    [Fact]
    public async Task RestoreAsync_restores_known_entries_from_backup()
    {
        var options = CreateOptions();
        Directory.CreateDirectory(options.UploadStoragePath);
        await File.WriteAllTextAsync(options.ProjectStorePath, """{"projects":[{"name":"before"}]}""");
        await File.WriteAllTextAsync(options.AuditLogPath, """{"events":[]}""");
        await File.WriteAllTextAsync(Path.Combine(options.UploadStoragePath, "manual.md"), "before");
        var service = new PersistenceBackupService(options);
        var backup = await service.CreateAsync(CancellationToken.None);

        await File.WriteAllTextAsync(options.ProjectStorePath, """{"projects":[]}""");
        await File.WriteAllTextAsync(Path.Combine(options.UploadStoragePath, "manual.md"), "after");
        var restore = await service.RestoreAsync(backup.FileName, CancellationToken.None);

        Assert.NotNull(restore);
        Assert.Contains("projects.json", restore.RestoredEntries);
        Assert.Contains("uploads/manual.md", restore.RestoredEntries);
        Assert.Contains("before", await File.ReadAllTextAsync(options.ProjectStorePath));
        Assert.Equal("before", await File.ReadAllTextAsync(Path.Combine(options.UploadStoragePath, "manual.md")));
    }

    [Fact]
    public async Task PreviewAsync_reports_manifest_and_backup_contents()
    {
        var options = CreateOptions();
        Directory.CreateDirectory(options.UploadStoragePath);
        await File.WriteAllTextAsync(options.ProjectStorePath, """{"projects":[]}""");
        await File.WriteAllTextAsync(options.AuditLogPath, """{"events":[]}""");
        await File.WriteAllTextAsync(Path.Combine(options.UploadStoragePath, "manual.md"), "# Manual");
        var service = new PersistenceBackupService(options);
        var backup = await service.CreateAsync(CancellationToken.None);

        var preview = await service.PreviewAsync(backup.FileName, CancellationToken.None);

        Assert.NotNull(preview);
        Assert.Equal("json-local-v1", preview.Format);
        Assert.True(preview.ContainsProjects);
        Assert.True(preview.ContainsAuditLog);
        Assert.Equal(1, preview.UploadFileCount);
        Assert.Empty(preview.Warnings);
        Assert.Contains("uploads/manual.md", preview.Entries);
    }

    [Fact]
    public async Task PreviewAsync_warns_when_required_entries_are_missing()
    {
        var options = CreateOptions();
        Directory.CreateDirectory(options.BackupPath);
        var backupPath = Path.Combine(options.BackupPath, "partial.zip");
        using (var archive = System.IO.Compression.ZipFile.Open(backupPath, System.IO.Compression.ZipArchiveMode.Create))
        {
            archive.CreateEntry("uploads/manual.md");
        }

        var service = new PersistenceBackupService(options);

        var preview = await service.PreviewAsync("partial.zip", CancellationToken.None);

        Assert.NotNull(preview);
        Assert.Contains(preview.Warnings, warning => warning.Contains("project data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.Warnings, warning => warning.Contains("audit log", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.Warnings, warning => warning.Contains("format", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RestoreAsync_rejects_path_like_backup_names()
    {
        var service = new PersistenceBackupService(CreateOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RestoreAsync("../backup.zip", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_writes_operational_log_entries()
    {
        var logger = new TestLogger<PersistenceBackupService>();
        var options = CreateOptions();
        var service = new PersistenceBackupService(options, logger);

        var backup = await service.CreateAsync(CancellationToken.None);

        Assert.True(File.Exists(backup.Path));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Created persistence backup", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RestoreAsync_writes_operational_log_entries()
    {
        var logger = new TestLogger<PersistenceBackupService>();
        var options = CreateOptions();
        await File.WriteAllTextAsync(options.ProjectStorePath, """{"projects":[]}""");
        await File.WriteAllTextAsync(options.AuditLogPath, """{"events":[]}""");
        var service = new PersistenceBackupService(options, logger);
        var backup = await service.CreateAsync(CancellationToken.None);

        await service.RestoreAsync(backup.FileName, CancellationToken.None);

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("Restoring local persistence", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Restored", StringComparison.Ordinal));
    }

    private static PersistenceBackupOptions CreateOptions()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-backup-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        return new PersistenceBackupOptions(
            root,
            Path.Combine(root, "uploads"),
            Path.Combine(root, "projects.json"),
            Path.Combine(root, "audit-log.json"),
            Path.Combine(root, "backups"));
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyCollection<LogEntry> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
