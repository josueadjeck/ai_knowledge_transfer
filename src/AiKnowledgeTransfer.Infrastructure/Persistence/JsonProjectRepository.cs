namespace AiKnowledgeTransfer.Infrastructure.Persistence;

using System.Collections.Concurrent;
using System.Text.Json;
using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;
using AiKnowledgeTransfer.Domain.Roadmaps;

public sealed class JsonProjectRepository : IProjectRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ConcurrentDictionary<Guid, KnowledgeProject> _projects;
    private readonly string _filePath;

    public JsonProjectRepository(string filePath)
    {
        _filePath = filePath;
        _projects = new ConcurrentDictionary<Guid, KnowledgeProject>(
            LoadProjects(filePath).ToDictionary(project => project.Id));
    }

    public Task<IReadOnlyCollection<KnowledgeProject>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<KnowledgeProject> projects = _projects.Values
            .OrderByDescending(project => project.CreatedAt)
            .ToArray();

        return Task.FromResult(projects);
    }

    public Task<KnowledgeProject?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _projects.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    public Task AddAsync(KnowledgeProject project, CancellationToken cancellationToken)
    {
        _projects.TryAdd(project.Id, project);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var snapshot = new ProjectStoreSnapshot(
            _projects.Values
                .OrderBy(project => project.CreatedAt)
                .Select(ToSnapshot)
                .ToArray());

        var temporaryPath = $"{_filePath}.tmp";
        await using (var fileStream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(fileStream, snapshot, JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private static IReadOnlyCollection<KnowledgeProject> LoadProjects(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var fileStream = File.OpenRead(filePath);
        var snapshot = JsonSerializer.Deserialize<ProjectStoreSnapshot>(fileStream, JsonOptions);
        return snapshot?.Projects.Select(FromSnapshot).ToArray() ?? [];
    }

    private static ProjectSnapshot ToSnapshot(KnowledgeProject project)
    {
        return new ProjectSnapshot(
            project.Id,
            project.Name,
            project.Description,
            project.Owner,
            project.CreatedAt,
            project.Documents.Select(ToSnapshot).ToArray(),
            project.KnowledgeItems.Select(ToSnapshot).ToArray(),
            project.Roadmaps.Select(ToSnapshot).ToArray());
    }

    private static DocumentSnapshot ToSnapshot(DocumentVersion document)
    {
        return new DocumentSnapshot(
            document.Id,
            document.FileName,
            document.ContentType,
            document.Source,
            document.SizeInBytes,
            document.VersionNumber,
            document.StoragePath,
            document.UploadedAt,
            document.Status,
            document.Chunks.Select(ToSnapshot).ToArray());
    }

    private static DocumentChunkSnapshot ToSnapshot(DocumentChunk chunk)
    {
        return new DocumentChunkSnapshot(
            chunk.Id,
            chunk.ChunkNumber,
            chunk.Text,
            chunk.StartCharacter,
            chunk.EndCharacter,
            chunk.SourceReference,
            chunk.CreatedAt);
    }

    private static KnowledgeItemSnapshot ToSnapshot(KnowledgeItem item)
    {
        return new KnowledgeItemSnapshot(
            item.Id,
            item.Type,
            item.Title,
            item.Summary,
            item.SourceDocumentId,
            item.ReviewStatus,
            item.CreatedAt,
            item.ReviewedBy,
            item.ReviewComment,
            item.ReviewedAt,
            item.SourceChunkNumber,
            item.ExtractionProvider,
            item.ExtractionModel,
            item.ExtractionUsedFallback,
            item.ExtractionQuality,
            item.ReviewHistory.Select(ToSnapshot).ToArray());
    }

    private static KnowledgeReviewHistorySnapshot ToSnapshot(KnowledgeReviewHistoryEntry history)
    {
        return new KnowledgeReviewHistorySnapshot(
            history.Id,
            history.Action,
            history.ReviewStatus,
            history.QualityStatus,
            history.Reviewer,
            history.Comment,
            history.CreatedAt);
    }

    private static RoadmapSnapshot ToSnapshot(OnboardingRoadmap roadmap)
    {
        return new RoadmapSnapshot(
            roadmap.Id,
            roadmap.TargetRole,
            roadmap.DurationInWeeks,
            roadmap.CreatedAt,
            roadmap.Status,
            roadmap.Weeks.Select(ToSnapshot).ToArray());
    }

    private static RoadmapWeekSnapshot ToSnapshot(RoadmapWeek week)
    {
        return new RoadmapWeekSnapshot(
            week.WeekNumber,
            week.Theme,
            week.LearningGoals.ToArray(),
            week.Exercises.ToArray(),
            week.AcceptanceCriteria.ToArray(),
            week.ReviewNotes.ToArray());
    }

    private static KnowledgeProject FromSnapshot(ProjectSnapshot snapshot)
    {
        return KnowledgeProject.Rehydrate(
            snapshot.Id,
            snapshot.Name,
            snapshot.Description,
            snapshot.Owner,
            snapshot.CreatedAt,
            snapshot.Documents.Select(FromSnapshot),
            snapshot.KnowledgeItems.Select(FromSnapshot),
            snapshot.Roadmaps.Select(FromSnapshot));
    }

    private static DocumentVersion FromSnapshot(DocumentSnapshot snapshot)
    {
        return DocumentVersion.Rehydrate(
            snapshot.Id,
            snapshot.FileName,
            snapshot.ContentType,
            snapshot.Source,
            snapshot.SizeInBytes,
            snapshot.VersionNumber,
            snapshot.StoragePath,
            snapshot.UploadedAt,
            snapshot.Status,
            snapshot.Chunks.Select(FromSnapshot));
    }

    private static DocumentChunk FromSnapshot(DocumentChunkSnapshot snapshot)
    {
        return DocumentChunk.Rehydrate(
            snapshot.Id,
            snapshot.ChunkNumber,
            snapshot.Text,
            snapshot.StartCharacter,
            snapshot.EndCharacter,
            snapshot.CreatedAt,
            snapshot.SourceReference);
    }

    private static KnowledgeItem FromSnapshot(KnowledgeItemSnapshot snapshot)
    {
        return KnowledgeItem.Rehydrate(
            snapshot.Id,
            snapshot.Type,
            snapshot.Title,
            snapshot.Summary,
            snapshot.SourceDocumentId,
            snapshot.SourceChunkNumber,
            snapshot.ExtractionProvider,
            snapshot.ExtractionModel,
            snapshot.ExtractionUsedFallback,
            snapshot.ExtractionQuality,
            snapshot.ReviewStatus,
            snapshot.CreatedAt,
            snapshot.ReviewedBy,
            snapshot.ReviewComment,
            snapshot.ReviewedAt,
            (snapshot.ReviewHistory ?? []).Select(FromSnapshot));
    }

    private static KnowledgeReviewHistoryEntry FromSnapshot(KnowledgeReviewHistorySnapshot snapshot)
    {
        return new KnowledgeReviewHistoryEntry(
            snapshot.Id,
            snapshot.Action,
            snapshot.ReviewStatus,
            snapshot.QualityStatus,
            snapshot.Reviewer,
            snapshot.Comment,
            snapshot.CreatedAt);
    }

    private static OnboardingRoadmap FromSnapshot(RoadmapSnapshot snapshot)
    {
        return OnboardingRoadmap.Rehydrate(
            snapshot.Id,
            snapshot.TargetRole,
            snapshot.DurationInWeeks,
            snapshot.CreatedAt,
            snapshot.Status,
            snapshot.Weeks.Select(FromSnapshot));
    }

    private static RoadmapWeek FromSnapshot(RoadmapWeekSnapshot snapshot)
    {
        return new RoadmapWeek(
            snapshot.WeekNumber,
            snapshot.Theme,
            snapshot.LearningGoals,
            snapshot.Exercises,
            snapshot.AcceptanceCriteria,
            snapshot.ReviewNotes);
    }

    private sealed record ProjectStoreSnapshot(IReadOnlyCollection<ProjectSnapshot> Projects);

    private sealed record ProjectSnapshot(
        Guid Id,
        string Name,
        string Description,
        string Owner,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<DocumentSnapshot> Documents,
        IReadOnlyCollection<KnowledgeItemSnapshot> KnowledgeItems,
        IReadOnlyCollection<RoadmapSnapshot> Roadmaps);

    private sealed record DocumentSnapshot(
        Guid Id,
        string FileName,
        string ContentType,
        string Source,
        long SizeInBytes,
        int VersionNumber,
        string StoragePath,
        DateTimeOffset UploadedAt,
        DocumentStatus Status,
        IReadOnlyCollection<DocumentChunkSnapshot> Chunks);

    private sealed record DocumentChunkSnapshot(
        Guid Id,
        int ChunkNumber,
        string Text,
        int StartCharacter,
        int EndCharacter,
        string SourceReference,
        DateTimeOffset CreatedAt);

    private sealed record KnowledgeItemSnapshot(
        Guid Id,
        KnowledgeItemType Type,
        string Title,
        string Summary,
        Guid? SourceDocumentId,
        KnowledgeReviewStatus ReviewStatus,
        DateTimeOffset CreatedAt,
        string? ReviewedBy,
        string? ReviewComment,
        DateTimeOffset? ReviewedAt,
        int? SourceChunkNumber = null,
        string ExtractionProvider = "Legacy",
        string? ExtractionModel = null,
        bool ExtractionUsedFallback = false,
        string ExtractionQuality = "LegacyImported",
        IReadOnlyCollection<KnowledgeReviewHistorySnapshot>? ReviewHistory = null);

    private sealed record KnowledgeReviewHistorySnapshot(
        Guid Id,
        string Action,
        KnowledgeReviewStatus ReviewStatus,
        string QualityStatus,
        string Reviewer,
        string Comment,
        DateTimeOffset CreatedAt);

    private sealed record RoadmapSnapshot(
        Guid Id,
        string TargetRole,
        int DurationInWeeks,
        DateTimeOffset CreatedAt,
        RoadmapStatus Status,
        IReadOnlyCollection<RoadmapWeekSnapshot> Weeks);

    private sealed record RoadmapWeekSnapshot(
        int WeekNumber,
        string Theme,
        IReadOnlyCollection<string> LearningGoals,
        IReadOnlyCollection<string> Exercises,
        IReadOnlyCollection<string> AcceptanceCriteria,
        IReadOnlyCollection<string> ReviewNotes);
}
