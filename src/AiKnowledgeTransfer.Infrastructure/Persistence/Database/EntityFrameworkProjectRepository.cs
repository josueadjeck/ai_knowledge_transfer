using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;
using AiKnowledgeTransfer.Domain.Roadmaps;
using Microsoft.EntityFrameworkCore;

namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class EntityFrameworkProjectRepository : IProjectRepository
{
    private readonly KnowledgeTransferDbContext _dbContext;
    private readonly Dictionary<Guid, KnowledgeProject> _trackedProjects = [];

    public EntityFrameworkProjectRepository(KnowledgeTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<KnowledgeProject>> ListAsync(CancellationToken cancellationToken)
    {
        var records = await QueryProjects()
            .OrderByDescending(project => project.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task<KnowledgeProject?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_trackedProjects.TryGetValue(id, out var trackedProject))
        {
            return trackedProject;
        }

        var record = await QueryProjects()
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var project = ToDomain(record);
        _trackedProjects[project.Id] = project;
        return project;
    }

    public Task AddAsync(KnowledgeProject project, CancellationToken cancellationToken)
    {
        _trackedProjects[project.Id] = project;
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var project in _trackedProjects.Values)
        {
            var existing = await QueryProjects()
                .SingleOrDefaultAsync(record => record.Id == project.Id, cancellationToken);
            if (existing is not null)
            {
                _dbContext.Projects.Remove(existing);
            }

            await _dbContext.Projects.AddAsync(ToRecord(project), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ProjectRecord> QueryProjects()
    {
        return _dbContext.Projects
            .Include(project => project.Documents)
                .ThenInclude(document => document.Chunks)
            .Include(project => project.KnowledgeItems)
            .Include(project => project.Roadmaps)
                .ThenInclude(roadmap => roadmap.Weeks)
                    .ThenInclude(week => week.TextItems);
    }

    private static ProjectRecord ToRecord(KnowledgeProject project)
    {
        return new ProjectRecord
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Owner = project.Owner,
            CreatedAt = project.CreatedAt,
            Documents = project.Documents.Select(ToRecord).ToList(),
            KnowledgeItems = project.KnowledgeItems.Select(ToRecord).ToList(),
            Roadmaps = project.Roadmaps.Select(ToRecord).ToList()
        };
    }

    private static DocumentRecord ToRecord(DocumentVersion document)
    {
        return new DocumentRecord
        {
            Id = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            Source = document.Source,
            SizeInBytes = document.SizeInBytes,
            VersionNumber = document.VersionNumber,
            StoragePath = document.StoragePath,
            UploadedAt = document.UploadedAt,
            Status = document.Status.ToString(),
            Chunks = document.Chunks.Select(ToRecord).ToList()
        };
    }

    private static DocumentChunkRecord ToRecord(DocumentChunk chunk)
    {
        return new DocumentChunkRecord
        {
            Id = chunk.Id,
            ChunkNumber = chunk.ChunkNumber,
            Text = chunk.Text,
            StartCharacter = chunk.StartCharacter,
            EndCharacter = chunk.EndCharacter,
            CreatedAt = chunk.CreatedAt
        };
    }

    private static KnowledgeItemRecord ToRecord(KnowledgeItem item)
    {
        return new KnowledgeItemRecord
        {
            Id = item.Id,
            Type = item.Type.ToString(),
            Title = item.Title,
            Summary = item.Summary,
            SourceDocumentId = item.SourceDocumentId,
            ReviewStatus = item.ReviewStatus.ToString(),
            CreatedAt = item.CreatedAt,
            ReviewedBy = item.ReviewedBy,
            ReviewComment = item.ReviewComment,
            ReviewedAt = item.ReviewedAt
        };
    }

    private static RoadmapRecord ToRecord(OnboardingRoadmap roadmap)
    {
        return new RoadmapRecord
        {
            Id = roadmap.Id,
            TargetRole = roadmap.TargetRole,
            DurationInWeeks = roadmap.DurationInWeeks,
            CreatedAt = roadmap.CreatedAt,
            Status = roadmap.Status.ToString(),
            Weeks = roadmap.Weeks.Select(ToRecord).ToList()
        };
    }

    private static RoadmapWeekRecord ToRecord(RoadmapWeek week)
    {
        var textItems = week.LearningGoals.Select((text, index) => ToRecord("LearningGoal", index, text))
            .Concat(week.Exercises.Select((text, index) => ToRecord("Exercise", index, text)))
            .Concat(week.AcceptanceCriteria.Select((text, index) => ToRecord("AcceptanceCriterion", index, text)))
            .Concat(week.ReviewNotes.Select((text, index) => ToRecord("ReviewNote", index, text)))
            .ToList();

        return new RoadmapWeekRecord
        {
            Id = Guid.NewGuid(),
            WeekNumber = week.WeekNumber,
            Theme = week.Theme,
            TextItems = textItems
        };
    }

    private static RoadmapWeekTextRecord ToRecord(string category, int sortOrder, string text)
    {
        return new RoadmapWeekTextRecord
        {
            Id = Guid.NewGuid(),
            Category = category,
            SortOrder = sortOrder,
            Text = text
        };
    }

    private static KnowledgeProject ToDomain(ProjectRecord record)
    {
        return KnowledgeProject.Rehydrate(
            record.Id,
            record.Name,
            record.Description,
            record.Owner,
            record.CreatedAt,
            record.Documents.OrderBy(document => document.UploadedAt).Select(ToDomain),
            record.KnowledgeItems.OrderBy(item => item.CreatedAt).Select(ToDomain),
            record.Roadmaps.OrderBy(roadmap => roadmap.CreatedAt).Select(ToDomain));
    }

    private static DocumentVersion ToDomain(DocumentRecord record)
    {
        return DocumentVersion.Rehydrate(
            record.Id,
            record.FileName,
            record.ContentType,
            record.Source,
            record.SizeInBytes,
            record.VersionNumber,
            record.StoragePath,
            record.UploadedAt,
            Enum.Parse<DocumentStatus>(record.Status),
            record.Chunks.OrderBy(chunk => chunk.ChunkNumber).Select(ToDomain));
    }

    private static DocumentChunk ToDomain(DocumentChunkRecord record)
    {
        return DocumentChunk.Rehydrate(
            record.Id,
            record.ChunkNumber,
            record.Text,
            record.StartCharacter,
            record.EndCharacter,
            record.CreatedAt);
    }

    private static KnowledgeItem ToDomain(KnowledgeItemRecord record)
    {
        return KnowledgeItem.Rehydrate(
            record.Id,
            Enum.Parse<KnowledgeItemType>(record.Type),
            record.Title,
            record.Summary,
            record.SourceDocumentId,
            Enum.Parse<KnowledgeReviewStatus>(record.ReviewStatus),
            record.CreatedAt,
            record.ReviewedBy,
            record.ReviewComment,
            record.ReviewedAt);
    }

    private static OnboardingRoadmap ToDomain(RoadmapRecord record)
    {
        return OnboardingRoadmap.Rehydrate(
            record.Id,
            record.TargetRole,
            record.DurationInWeeks,
            record.CreatedAt,
            Enum.Parse<RoadmapStatus>(record.Status),
            record.Weeks.OrderBy(week => week.WeekNumber).Select(ToDomain));
    }

    private static RoadmapWeek ToDomain(RoadmapWeekRecord record)
    {
        return new RoadmapWeek(
            record.WeekNumber,
            record.Theme,
            GetTexts(record, "LearningGoal"),
            GetTexts(record, "Exercise"),
            GetTexts(record, "AcceptanceCriterion"),
            GetTexts(record, "ReviewNote"));
    }

    private static IReadOnlyCollection<string> GetTexts(RoadmapWeekRecord record, string category)
    {
        return record.TextItems
            .Where(item => item.Category == category)
            .OrderBy(item => item.SortOrder)
            .Select(item => item.Text)
            .ToArray();
    }
}
