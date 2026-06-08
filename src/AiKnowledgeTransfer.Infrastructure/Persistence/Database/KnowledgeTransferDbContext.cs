using Microsoft.EntityFrameworkCore;

namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class KnowledgeTransferDbContext : DbContext
{
    public KnowledgeTransferDbContext(DbContextOptions<KnowledgeTransferDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    public DbSet<DocumentChunkRecord> DocumentChunks => Set<DocumentChunkRecord>();

    public DbSet<KnowledgeItemRecord> KnowledgeItems => Set<KnowledgeItemRecord>();

    public DbSet<KnowledgeReviewHistoryRecord> KnowledgeReviewHistory => Set<KnowledgeReviewHistoryRecord>();

    public DbSet<RoadmapRecord> Roadmaps => Set<RoadmapRecord>();

    public DbSet<RoadmapWeekRecord> RoadmapWeeks => Set<RoadmapWeekRecord>();

    public DbSet<RoadmapWeekTextRecord> RoadmapWeekTextItems => Set<RoadmapWeekTextRecord>();

    public DbSet<AuditEventRecord> AuditEvents => Set<AuditEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectRecord>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(240).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(2000).IsRequired();
            entity.Property(project => project.Owner).HasMaxLength(240).IsRequired();
            entity.HasMany(project => project.Documents).WithOne(document => document.Project).HasForeignKey(document => document.ProjectId);
            entity.HasMany(project => project.KnowledgeItems).WithOne(item => item.Project).HasForeignKey(item => item.ProjectId);
            entity.HasMany(project => project.Roadmaps).WithOne(roadmap => roadmap.Project).HasForeignKey(roadmap => roadmap.ProjectId);
        });

        modelBuilder.Entity<DocumentRecord>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.FileName).HasMaxLength(512).IsRequired();
            entity.Property(document => document.ContentType).HasMaxLength(160).IsRequired();
            entity.Property(document => document.Source).HasMaxLength(512).IsRequired();
            entity.Property(document => document.StoragePath).HasMaxLength(1024).IsRequired();
            entity.Property(document => document.Status).HasMaxLength(80).IsRequired();
            entity.HasMany(document => document.Chunks).WithOne(chunk => chunk.Document).HasForeignKey(chunk => chunk.DocumentId);
        });

        modelBuilder.Entity<DocumentChunkRecord>(entity =>
        {
            entity.ToTable("DocumentChunks");
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Text).IsRequired();
            entity.Property(chunk => chunk.SourceReference).HasMaxLength(512).IsRequired();
            entity.Property(chunk => chunk.QualityStatus).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<KnowledgeItemRecord>(entity =>
        {
            entity.ToTable("KnowledgeItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(512).IsRequired();
            entity.Property(item => item.Summary).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.ExtractionProvider).HasMaxLength(160).IsRequired();
            entity.Property(item => item.ExtractionModel).HasMaxLength(240);
            entity.Property(item => item.ExtractionQuality).HasMaxLength(120).IsRequired();
            entity.Property(item => item.ReviewStatus).HasMaxLength(80).IsRequired();
            entity.Property(item => item.ReviewedBy).HasMaxLength(240);
            entity.Property(item => item.ReviewComment).HasMaxLength(2000);
            entity.HasMany(item => item.ReviewHistory).WithOne(history => history.KnowledgeItem).HasForeignKey(history => history.KnowledgeItemId);
        });

        modelBuilder.Entity<KnowledgeReviewHistoryRecord>(entity =>
        {
            entity.ToTable("KnowledgeReviewHistory");
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Action).HasMaxLength(160).IsRequired();
            entity.Property(history => history.ReviewStatus).HasMaxLength(80).IsRequired();
            entity.Property(history => history.QualityStatus).HasMaxLength(120).IsRequired();
            entity.Property(history => history.Reviewer).HasMaxLength(240).IsRequired();
            entity.Property(history => history.Comment).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<RoadmapRecord>(entity =>
        {
            entity.ToTable("Roadmaps");
            entity.HasKey(roadmap => roadmap.Id);
            entity.Property(roadmap => roadmap.TargetRole).HasMaxLength(240).IsRequired();
            entity.Property(roadmap => roadmap.Status).HasMaxLength(80).IsRequired();
            entity.HasMany(roadmap => roadmap.Weeks).WithOne(week => week.Roadmap).HasForeignKey(week => week.RoadmapId);
        });

        modelBuilder.Entity<RoadmapWeekRecord>(entity =>
        {
            entity.ToTable("RoadmapWeeks");
            entity.HasKey(week => week.Id);
            entity.Property(week => week.Theme).HasMaxLength(512).IsRequired();
            entity.HasMany(week => week.TextItems).WithOne(item => item.RoadmapWeek).HasForeignKey(item => item.RoadmapWeekId);
        });

        modelBuilder.Entity<RoadmapWeekTextRecord>(entity =>
        {
            entity.ToTable("RoadmapWeekTextItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Category).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Text).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<AuditEventRecord>(entity =>
        {
            entity.ToTable("AuditEvents");
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.Action).HasMaxLength(160).IsRequired();
            entity.Property(auditEvent => auditEvent.Actor).HasMaxLength(240).IsRequired();
            entity.Property(auditEvent => auditEvent.TargetType).HasMaxLength(160).IsRequired();
            entity.Property(auditEvent => auditEvent.Summary).HasMaxLength(2000).IsRequired();
        });
    }
}
