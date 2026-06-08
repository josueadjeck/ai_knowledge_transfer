namespace AiKnowledgeTransfer.Application.Exports;

using System.Text;
using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Contracts.Exports;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;

public sealed class MarkdownExportService(
    IProjectRepository projects,
    IAuditLog? auditLog = null)
{
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;

    public async Task<ExportProjectMarkdownResponse?> ExportProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var markdown = BuildMarkdown(project, generatedAt);
        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, "ProjectExported", "system", "Project", project.Id, $"Markdown export for '{project.Name}' was generated."),
            cancellationToken);

        return new ExportProjectMarkdownResponse(
            project.Id,
            $"{ToSafeFileName(project.Name)}-knowledge-transfer.md",
            "text/markdown",
            markdown,
            generatedAt);
    }

    private static string BuildMarkdown(KnowledgeProject project, DateTimeOffset generatedAt)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {project.Name} Knowledge Transfer");
        builder.AppendLine();
        builder.AppendLine($"Generated: {generatedAt:O}");
        builder.AppendLine($"Owner: {project.Owner}");
        builder.AppendLine();
        builder.AppendLine(project.Description);
        builder.AppendLine();

        AppendDocuments(builder, project);
        AppendKnowledge(builder, project);
        AppendTraceability(builder, project);
        AppendRoadmaps(builder, project);

        return builder.ToString();
    }

    private static void AppendDocuments(StringBuilder builder, KnowledgeProject project)
    {
        builder.AppendLine("## Sources");
        builder.AppendLine();

        if (project.Documents.Count == 0)
        {
            builder.AppendLine("No documents registered.");
            builder.AppendLine();
            return;
        }

        foreach (var document in project.Documents.OrderBy(document => document.FileName).ThenBy(document => document.VersionNumber))
        {
            builder.AppendLine($"- {document.FileName} v{document.VersionNumber} ({document.Status})");
            builder.AppendLine($"  - Source: {document.Source}");
            builder.AppendLine($"  - Content type: {document.ContentType}");
            builder.AppendLine($"  - Chunks: {document.Chunks.Count}");
        }

        builder.AppendLine();
    }

    private static void AppendKnowledge(StringBuilder builder, KnowledgeProject project)
    {
        builder.AppendLine("## Knowledge Items");
        builder.AppendLine();

        var approvedItems = project.KnowledgeItems
            .Where(KnowledgeQualityPolicy.IsFinal)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Title)
            .ToArray();

        if (approvedItems.Length == 0)
        {
            builder.AppendLine("No verified knowledge items available.");
        }
        else
        {
            foreach (var item in approvedItems)
            {
                builder.AppendLine($"### {item.Title}");
                builder.AppendLine();
                builder.AppendLine($"- Type: {item.Type}");
                builder.AppendLine($"- Status: {item.ReviewStatus}");
                builder.AppendLine($"- Reviewed by: {item.ReviewedBy}");
                builder.AppendLine($"- Source chunk: {FormatSourceChunk(item)}");
                builder.AppendLine($"- Extraction: {FormatExtraction(item)}");
                builder.AppendLine($"- Quality: {item.ExtractionQuality}");
                builder.AppendLine($"- Summary: {item.Summary}");
                builder.AppendLine();
            }
        }

        var reviewItems = project.KnowledgeItems
            .Where(item => !KnowledgeQualityPolicy.IsFinal(item))
            .OrderBy(item => item.ReviewStatus)
            .ThenBy(item => item.Title)
            .ToArray();

        builder.AppendLine();
        builder.AppendLine("## Review Notes");
        builder.AppendLine();

        if (reviewItems.Length == 0)
        {
            builder.AppendLine("No open review notes.");
            builder.AppendLine();
            return;
        }

        foreach (var item in reviewItems)
        {
            builder.AppendLine($"- {item.Title} ({item.Type}, {item.ReviewStatus}, {item.ExtractionQuality}): {item.Summary}");
        }

        builder.AppendLine();
    }

    private static void AppendRoadmaps(StringBuilder builder, KnowledgeProject project)
    {
        builder.AppendLine("## Roadmaps");
        builder.AppendLine();

        if (project.Roadmaps.Count == 0)
        {
            builder.AppendLine("No roadmaps generated.");
            builder.AppendLine();
            return;
        }

        foreach (var roadmap in project.Roadmaps.OrderByDescending(roadmap => roadmap.CreatedAt))
        {
            builder.AppendLine($"### {roadmap.TargetRole} ({roadmap.DurationInWeeks} weeks, {roadmap.Status})");
            builder.AppendLine();

            foreach (var week in roadmap.Weeks.OrderBy(week => week.WeekNumber))
            {
                builder.AppendLine($"#### Week {week.WeekNumber}: {week.Theme}");
                builder.AppendLine();
                AppendList(builder, "Learning goals", week.LearningGoals);
                AppendList(builder, "Exercises", week.Exercises);
                AppendList(builder, "Acceptance criteria", week.AcceptanceCriteria);
                AppendList(builder, "Review notes", week.ReviewNotes);
            }
        }
    }

    private static void AppendTraceability(StringBuilder builder, KnowledgeProject project)
    {
        builder.AppendLine("## Traceability Matrix");
        builder.AppendLine();
        builder.AppendLine("| Source | Chunks | Knowledge Item | Type | Review Status | Extraction | Quality | Reviewed By | Roadmap Usage | Exported |");
        builder.AppendLine("| --- | ---: | --- | --- | --- | --- | --- | --- | ---: | --- |");

        if (project.Documents.Count == 0)
        {
            builder.AppendLine("| - | 0 | - | - | - | - | - | - | 0 | No |");
            builder.AppendLine();
            return;
        }

        foreach (var document in project.Documents.OrderBy(document => document.FileName).ThenBy(document => document.VersionNumber))
        {
            var linkedItems = project.KnowledgeItems
                .Where(item => item.SourceDocumentId == document.Id)
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Title)
                .ToArray();

            if (linkedItems.Length == 0)
            {
                builder.AppendLine($"| {EscapeTable(document.FileName)} | {document.Chunks.Count} | - | - | - | - | - | - | 0 | No |");
                continue;
            }

            foreach (var item in linkedItems)
            {
                var roadmapUsage = project.Roadmaps
                    .SelectMany(roadmap => roadmap.Weeks)
                    .Count(week =>
                        ContainsTitle(week.LearningGoals, item.Title)
                        || ContainsTitle(week.Exercises, item.Title)
                        || ContainsTitle(week.ReviewNotes, item.Title));

                var exported = KnowledgeQualityPolicy.IsFinal(item) ? "Yes" : "No";

                builder.AppendLine(
                    $"| {EscapeTable(document.FileName)} | {document.Chunks.Count} | {EscapeTable(item.Title)} | {item.Type} | {item.ReviewStatus} | {EscapeTable(FormatExtraction(item))} | {item.ExtractionQuality} | {EscapeTable(item.ReviewedBy ?? "-")} | {roadmapUsage} | {exported} |");
            }
        }

        builder.AppendLine();
    }

    private static void AppendList(StringBuilder builder, string title, IReadOnlyCollection<string> items)
    {
        builder.AppendLine($"**{title}**");
        builder.AppendLine();

        if (items.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
    }

    private static string ToSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(safeName) ? "knowledge-transfer" : safeName.Trim();
    }

    private static bool ContainsTitle(IEnumerable<string> values, string title)
    {
        return values.Any(value => value.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatSourceChunk(KnowledgeItem item)
    {
        return item.SourceChunkNumber is null ? "-" : item.SourceChunkNumber.Value.ToString();
    }

    private static string FormatExtraction(KnowledgeItem item)
    {
        var model = string.IsNullOrWhiteSpace(item.ExtractionModel) ? string.Empty : $" ({item.ExtractionModel})";
        var fallback = item.ExtractionUsedFallback ? ", fallback" : string.Empty;
        return $"{item.ExtractionProvider}{model}{fallback}";
    }

    private static string EscapeTable(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
