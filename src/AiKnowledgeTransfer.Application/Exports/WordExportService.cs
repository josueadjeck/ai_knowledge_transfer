namespace AiKnowledgeTransfer.Application.Exports;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Contracts.Exports;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

public sealed class WordExportService(
    IProjectRepository projects,
    IAuditLog? auditLog = null)
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;

    public async Task<ExportProjectWordResponse?> ExportProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var fileName = $"{ToSafeFileName(project.Name)}-knowledge-transfer.docx";
        var document = BuildDocument(project, generatedAt);

        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, "ProjectExported", "system", fileName, project.Id, $"Word export for '{project.Name}' was generated."),
            cancellationToken);

        return new ExportProjectWordResponse(
            project.Id,
            fileName,
            ContentType,
            document,
            generatedAt);
    }

    private static byte[] BuildDocument(KnowledgeProject project, DateTimeOffset generatedAt)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body ?? new Body();

            AddHeading(body, $"{project.Name} Knowledge Transfer", 1);
            AddParagraph(body, $"Generated: {generatedAt:O}");
            AddParagraph(body, $"Owner: {project.Owner}");
            AddParagraph(body, project.Description);

            AddSources(body, project);
            AddKnowledgeItems(body, project);
            AddReviewNotes(body, project);
            AddReviewHistory(body, project);
            AddTraceability(body, project);
            AddRoadmaps(body, project);

            mainPart.Document.Body = body;
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AddSources(Body body, KnowledgeProject project)
    {
        AddHeading(body, "Sources", 2);
        if (project.Documents.Count == 0)
        {
            AddParagraph(body, "No documents registered.");
            return;
        }

        foreach (var document in project.Documents.OrderBy(document => document.FileName).ThenBy(document => document.VersionNumber))
        {
            AddBullet(body, $"{document.FileName} v{document.VersionNumber} ({document.Status})");
            AddParagraph(body, $"Source: {document.Source}; Content type: {document.ContentType}; Chunks: {document.Chunks.Count}");
        }
    }

    private static void AddKnowledgeItems(Body body, KnowledgeProject project)
    {
        AddHeading(body, "Knowledge Items", 2);
        var finalItems = project.KnowledgeItems
            .Where(KnowledgeQualityPolicy.IsFinal)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Title)
            .ToArray();

        if (finalItems.Length == 0)
        {
            AddParagraph(body, "No verified knowledge items available.");
            return;
        }

        foreach (var item in finalItems)
        {
            AddHeading(body, item.Title, 3);
            AddBullet(body, $"Type: {item.Type}");
            AddBullet(body, $"Status: {item.ReviewStatus}");
            AddBullet(body, $"Reviewed by: {item.ReviewedBy ?? "-"}");
            AddBullet(body, $"Source chunk: {(item.SourceChunkNumber is null ? "-" : item.SourceChunkNumber.Value)}");
            AddBullet(body, $"Extraction: {FormatExtraction(item)}");
            AddBullet(body, $"Quality: {item.ExtractionQuality}");
            AddParagraph(body, item.Summary);
        }
    }

    private static void AddReviewNotes(Body body, KnowledgeProject project)
    {
        AddHeading(body, "Review Notes", 2);
        var reviewItems = project.KnowledgeItems
            .Where(item => !KnowledgeQualityPolicy.IsFinal(item))
            .OrderBy(item => item.ReviewStatus)
            .ThenBy(item => item.Title)
            .ToArray();

        if (reviewItems.Length == 0)
        {
            AddParagraph(body, "No open review notes.");
            return;
        }

        foreach (var item in reviewItems)
        {
            AddBullet(body, $"{item.Title} ({item.Type}, {item.ReviewStatus}, {item.ExtractionQuality}): {item.Summary}");
        }
    }

    private static void AddReviewHistory(Body body, KnowledgeProject project)
    {
        AddHeading(body, "Review History", 2);
        var itemsWithHistory = project.KnowledgeItems
            .Where(item => item.ReviewHistory.Count > 0)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Title)
            .ToArray();

        if (itemsWithHistory.Length == 0)
        {
            AddParagraph(body, "No review history available.");
            return;
        }

        foreach (var item in itemsWithHistory)
        {
            AddHeading(body, item.Title, 3);
            foreach (var history in item.ReviewHistory.OrderBy(history => history.CreatedAt))
            {
                AddBullet(body, $"{history.CreatedAt:O} | {history.Action} | {history.ReviewStatus} | {history.QualityStatus} | {history.Reviewer} | {history.Comment}");
            }
        }
    }

    private static void AddTraceability(Body body, KnowledgeProject project)
    {
        AddHeading(body, "Traceability Matrix", 2);
        if (project.Documents.Count == 0)
        {
            AddParagraph(body, "No traceability data available.");
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
                AddBullet(body, $"{document.FileName}: no linked knowledge items.");
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

                AddBullet(body, $"{document.FileName} | {item.Title} | {item.Type} | {item.ReviewStatus} | {item.ExtractionQuality} | Roadmap usage: {roadmapUsage} | Exported: {exported}");
            }
        }
    }

    private static void AddRoadmaps(Body body, KnowledgeProject project)
    {
        AddHeading(body, "Roadmaps", 2);
        if (project.Roadmaps.Count == 0)
        {
            AddParagraph(body, "No roadmaps generated.");
            return;
        }

        foreach (var roadmap in project.Roadmaps.OrderByDescending(roadmap => roadmap.CreatedAt))
        {
            AddHeading(body, $"{roadmap.TargetRole} ({roadmap.DurationInWeeks} weeks, {roadmap.Status})", 3);
            foreach (var week in roadmap.Weeks.OrderBy(week => week.WeekNumber))
            {
                AddHeading(body, $"Week {week.WeekNumber}: {week.Theme}", 4);
                AddList(body, "Learning goals", week.LearningGoals);
                AddList(body, "Exercises", week.Exercises);
                AddList(body, "Acceptance criteria", week.AcceptanceCriteria);
                AddList(body, "Review notes", week.ReviewNotes);
            }
        }
    }

    private static void AddList(Body body, string title, IReadOnlyCollection<string> items)
    {
        AddParagraph(body, title);
        if (items.Count == 0)
        {
            AddBullet(body, "None");
            return;
        }

        foreach (var item in items)
        {
            AddBullet(body, item);
        }
    }

    private static void AddHeading(Body body, string text, int level)
    {
        body.AppendChild(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" }),
            new Run(new Text(text))));
    }

    private static void AddParagraph(Body body, string text)
    {
        body.AppendChild(new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddBullet(Body body, string text)
    {
        AddParagraph(body, $"- {text}");
    }

    private static bool ContainsTitle(IEnumerable<string> values, string title)
    {
        return values.Any(value => value.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatExtraction(KnowledgeItem item)
    {
        var model = string.IsNullOrWhiteSpace(item.ExtractionModel) ? string.Empty : $" ({item.ExtractionModel})";
        var fallback = item.ExtractionUsedFallback ? ", fallback" : string.Empty;
        return $"{item.ExtractionProvider}{model}{fallback}";
    }

    private static string ToSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(safeName) ? "knowledge-transfer" : safeName.Trim();
    }
}
