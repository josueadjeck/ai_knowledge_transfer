using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database.Migrations;

[DbContext(typeof(KnowledgeTransferDbContext))]
[Migration("20260609000100_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                Action = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Actor = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                TargetType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                TargetId = table.Column<Guid>(type: "TEXT", nullable: true),
                Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", auditEvent => auditEvent.Id);
            });

        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Owner = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", project => project.Id);
            });

        migrationBuilder.CreateTable(
            name: "Documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                SizeInBytes = table.Column<long>(type: "INTEGER", nullable: false),
                VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                StoragePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                UploadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documents", document => document.Id);
                table.ForeignKey(
                    name: "FK_Documents_Projects_ProjectId",
                    column: document => document.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "KnowledgeItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Summary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                SourceDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                SourceChunkNumber = table.Column<int>(type: "INTEGER", nullable: true),
                ExtractionProvider = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ExtractionModel = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                ExtractionUsedFallback = table.Column<bool>(type: "INTEGER", nullable: false),
                ExtractionQuality = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                ReviewStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                ReviewComment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                ReviewedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KnowledgeItems", item => item.Id);
                table.ForeignKey(
                    name: "FK_KnowledgeItems_Projects_ProjectId",
                    column: item => item.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Roadmaps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                TargetRole = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                DurationInWeeks = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roadmaps", roadmap => roadmap.Id);
                table.ForeignKey(
                    name: "FK_Roadmaps_Projects_ProjectId",
                    column: roadmap => roadmap.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DocumentChunks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                ChunkNumber = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", nullable: false),
                StartCharacter = table.Column<int>(type: "INTEGER", nullable: false),
                EndCharacter = table.Column<int>(type: "INTEGER", nullable: false),
                SourceReference = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                QualityStatus = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentChunks", chunk => chunk.Id);
                table.ForeignKey(
                    name: "FK_DocumentChunks_Documents_DocumentId",
                    column: chunk => chunk.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "KnowledgeReviewHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                KnowledgeItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                Action = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ReviewStatus = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                QualityStatus = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Reviewer = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KnowledgeReviewHistory", history => history.Id);
                table.ForeignKey(
                    name: "FK_KnowledgeReviewHistory_KnowledgeItems_KnowledgeItemId",
                    column: history => history.KnowledgeItemId,
                    principalTable: "KnowledgeItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoadmapWeeks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                RoadmapId = table.Column<Guid>(type: "TEXT", nullable: false),
                WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                Theme = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoadmapWeeks", week => week.Id);
                table.ForeignKey(
                    name: "FK_RoadmapWeeks_Roadmaps_RoadmapId",
                    column: week => week.RoadmapId,
                    principalTable: "Roadmaps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoadmapWeekTextItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                RoadmapWeekId = table.Column<Guid>(type: "TEXT", nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoadmapWeekTextItems", item => item.Id);
                table.ForeignKey(
                    name: "FK_RoadmapWeekTextItems_RoadmapWeeks_RoadmapWeekId",
                    column: item => item.RoadmapWeekId,
                    principalTable: "RoadmapWeeks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_DocumentChunks_DocumentId", "DocumentChunks", "DocumentId");
        migrationBuilder.CreateIndex("IX_Documents_ProjectId", "Documents", "ProjectId");
        migrationBuilder.CreateIndex("IX_KnowledgeItems_ProjectId", "KnowledgeItems", "ProjectId");
        migrationBuilder.CreateIndex("IX_KnowledgeReviewHistory_KnowledgeItemId", "KnowledgeReviewHistory", "KnowledgeItemId");
        migrationBuilder.CreateIndex("IX_Roadmaps_ProjectId", "Roadmaps", "ProjectId");
        migrationBuilder.CreateIndex("IX_RoadmapWeeks_RoadmapId", "RoadmapWeeks", "RoadmapId");
        migrationBuilder.CreateIndex("IX_RoadmapWeekTextItems_RoadmapWeekId", "RoadmapWeekTextItems", "RoadmapWeekId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AuditEvents");
        migrationBuilder.DropTable("DocumentChunks");
        migrationBuilder.DropTable("KnowledgeReviewHistory");
        migrationBuilder.DropTable("RoadmapWeekTextItems");
        migrationBuilder.DropTable("Documents");
        migrationBuilder.DropTable("KnowledgeItems");
        migrationBuilder.DropTable("RoadmapWeeks");
        migrationBuilder.DropTable("Roadmaps");
        migrationBuilder.DropTable("Projects");
    }
}
