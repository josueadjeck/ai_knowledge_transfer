using System.IO.Compression;
using System.Text.Json;
using AiKnowledgeTransfer.Contracts.Operations;
using Microsoft.Extensions.Logging;

namespace AiKnowledgeTransfer.Application.Operations;

public sealed class PersistenceBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly PersistenceBackupOptions _options;
    private readonly ILogger<PersistenceBackupService>? _logger;

    public PersistenceBackupService(
        PersistenceBackupOptions options,
        ILogger<PersistenceBackupService>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<BackupResponse> CreateAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.BackupPath);

        var createdAt = DateTimeOffset.UtcNow;
        var fileName = $"ai-knowledge-transfer-{createdAt:yyyyMMdd-HHmmss}.zip";
        var path = Path.Combine(_options.BackupPath, fileName);

        _logger?.LogInformation("Creating persistence backup {BackupFileName} in {BackupPath}.", fileName, _options.BackupPath);

        await using (var fileStream = File.Create(path))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
        {
            await WriteManifestAsync(archive, createdAt, cancellationToken);
            await AddFileIfExistsAsync(archive, _options.ProjectStorePath, "projects.json", cancellationToken);
            await AddFileIfExistsAsync(archive, _options.AuditLogPath, "audit-log.json", cancellationToken);
            await AddDirectoryIfExistsAsync(archive, _options.UploadStoragePath, "uploads", cancellationToken);
        }

        var fileInfo = new FileInfo(path);
        _logger?.LogInformation("Created persistence backup {BackupFileName} with {BackupSizeBytes} bytes.", fileName, fileInfo.Length);

        return new BackupResponse(fileName, path, createdAt, fileInfo.Length);
    }

    public IReadOnlyCollection<BackupResponse> List()
    {
        if (!Directory.Exists(_options.BackupPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(_options.BackupPath, "*.zip", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                var fileInfo = new FileInfo(path);
                return new BackupResponse(fileInfo.Name, fileInfo.FullName, fileInfo.CreationTimeUtc, fileInfo.Length);
            })
            .OrderByDescending(backup => backup.CreatedAt)
            .ToArray();
    }

    public Task<BackupPreviewResponse?> PreviewAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!IsSafeBackupFileName(fileName))
        {
            _logger?.LogWarning("Rejected backup preview for unsafe file name {BackupFileName}.", fileName);
            throw new InvalidOperationException("Backup file name is invalid.");
        }

        var backupPath = Path.Combine(_options.BackupPath, fileName);
        if (!File.Exists(backupPath))
        {
            _logger?.LogWarning("Backup preview requested for missing backup {BackupFileName}.", fileName);
            return Task.FromResult<BackupPreviewResponse?>(null);
        }

        var fileInfo = new FileInfo(backupPath);
        using var archive = ZipFile.OpenRead(backupPath);
        var entries = archive.Entries
            .Where(entry => !IsDirectoryEntry(entry))
            .Select(entry => entry.FullName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var manifest = ReadManifest(archive);
        var warnings = BuildPreviewWarnings(entries, manifest.Format);
        cancellationToken.ThrowIfCancellationRequested();

        if (warnings.Count > 0)
        {
            _logger?.LogWarning("Backup preview for {BackupFileName} reported {WarningCount} warnings.", fileName, warnings.Count);
        }
        else
        {
            _logger?.LogInformation("Backup preview for {BackupFileName} completed with {EntryCount} entries.", fileName, entries.Length);
        }

        return Task.FromResult<BackupPreviewResponse?>(new BackupPreviewResponse(
            fileName,
            manifest.CreatedAt,
            manifest.Format,
            entries.Length,
            fileInfo.Length,
            entries.Contains("projects.json", StringComparer.OrdinalIgnoreCase),
            entries.Contains("audit-log.json", StringComparer.OrdinalIgnoreCase),
            entries.Count(entry => entry.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)),
            entries.Take(25).ToArray(),
            warnings));
    }

    public async Task<RestoreResponse?> RestoreAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!IsSafeBackupFileName(fileName))
        {
            _logger?.LogWarning("Rejected restore for unsafe backup file name {BackupFileName}.", fileName);
            throw new InvalidOperationException("Backup file name is invalid.");
        }

        var backupPath = Path.Combine(_options.BackupPath, fileName);
        if (!File.Exists(backupPath))
        {
            _logger?.LogWarning("Restore requested for missing backup {BackupFileName}.", fileName);
            return null;
        }

        _logger?.LogWarning("Restoring local persistence from backup {BackupFileName}.", fileName);

        Directory.CreateDirectory(_options.AppDataPath);
        Directory.CreateDirectory(_options.UploadStoragePath);

        var restoredEntries = new List<string>();
        using var archive = ZipFile.OpenRead(backupPath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.FullName.Equals("projects.json", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractFileAsync(entry, _options.ProjectStorePath, cancellationToken);
                restoredEntries.Add(entry.FullName);
                continue;
            }

            if (entry.FullName.Equals("audit-log.json", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractFileAsync(entry, _options.AuditLogPath, cancellationToken);
                restoredEntries.Add(entry.FullName);
                continue;
            }

            if (entry.FullName.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) && !IsDirectoryEntry(entry))
            {
                var relativePath = entry.FullName["uploads/".Length..];
                var destinationPath = GetSafeUploadDestinationPath(relativePath);
                await ExtractFileAsync(entry, destinationPath, cancellationToken);
                restoredEntries.Add(entry.FullName);
            }
        }

        _logger?.LogInformation("Restored {RestoredEntryCount} entries from backup {BackupFileName}.", restoredEntries.Count, fileName);

        return new RestoreResponse(fileName, DateTimeOffset.UtcNow, restoredEntries);
    }

    private async Task WriteManifestAsync(ZipArchive archive, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var manifest = new
        {
            service = "AiKnowledgeTransfer",
            createdAt,
            format = "json-local-v1"
        };

        var entry = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
        await using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
    }

    private static BackupManifest ReadManifest(ZipArchive archive)
    {
        var manifestEntry = archive.GetEntry("manifest.json");
        if (manifestEntry is null)
        {
            return new BackupManifest(null, "unknown");
        }

        using var stream = manifestEntry.Open();
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var createdAt = root.TryGetProperty("createdAt", out var createdAtElement)
            && createdAtElement.TryGetDateTimeOffset(out var value)
                ? value
                : (DateTimeOffset?)null;
        var format = root.TryGetProperty("format", out var formatElement)
            ? formatElement.GetString() ?? "unknown"
            : "unknown";

        return new BackupManifest(createdAt, format);
    }

    private static IReadOnlyCollection<string> BuildPreviewWarnings(IReadOnlyCollection<string> entries, string format)
    {
        var warnings = new List<string>();

        if (!format.Equals("json-local-v1", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Backup format is unknown or unsupported.");
        }

        if (!entries.Contains("projects.json", StringComparer.OrdinalIgnoreCase))
        {
            warnings.Add("Backup does not contain project data.");
        }

        if (!entries.Contains("audit-log.json", StringComparer.OrdinalIgnoreCase))
        {
            warnings.Add("Backup does not contain audit log data.");
        }

        return warnings;
    }

    private static async Task AddFileIfExistsAsync(ZipArchive archive, string sourcePath, string entryName, CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            return;
        }

        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
        await using var source = File.OpenRead(sourcePath);
        await using var destination = entry.Open();
        await source.CopyToAsync(destination, cancellationToken);
    }

    private static async Task AddDirectoryIfExistsAsync(ZipArchive archive, string sourceDirectory, string entryPrefix, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath).Replace('\\', '/');
            var entry = archive.CreateEntry($"{entryPrefix}/{relativePath}", CompressionLevel.Fastest);
            await using var source = File.OpenRead(sourcePath);
            await using var destination = entry.Open();
            await source.CopyToAsync(destination, cancellationToken);
        }
    }

    private static async Task ExtractFileAsync(ZipArchiveEntry entry, string destinationPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var source = entry.Open();
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private string GetSafeUploadDestinationPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath)
            || relativePath.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Backup contains an invalid upload path.");
        }

        var destinationPath = Path.GetFullPath(Path.Combine(_options.UploadStoragePath, relativePath));
        var uploadRoot = Path.GetFullPath(_options.UploadStoragePath);
        if (!destinationPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Backup upload path escapes the storage directory.");
        }

        return destinationPath;
    }

    private static bool IsSafeBackupFileName(string fileName)
    {
        return fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && Path.GetFileName(fileName) == fileName;
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry)
    {
        return string.IsNullOrEmpty(entry.Name);
    }

    private sealed record BackupManifest(DateTimeOffset? CreatedAt, string Format);
}
