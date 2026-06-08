namespace AiKnowledgeTransfer.Application.Compliance;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Contracts.Compliance;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;

public sealed class ComplianceMatrixService(
    IProjectRepository projects,
    IAuditLog? auditLog = null)
{
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;

    public async Task<ComplianceMatrixResponse?> GetMatrixAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var auditEvents = await _auditLog.ListAsync(project.Id, cancellationToken);
        var hasExportApproval = auditEvents.Any(auditEvent => auditEvent.Action == "ExportApproved");
        var rows = BuildRows(project, hasExportApproval);
        var matrix = new ComplianceMatrixResponse(
            project.Id,
            project.Name,
            DateTimeOffset.UtcNow,
            rows.Count,
            rows.Count(row => row.ComplianceStatus == "Compliant"),
            rows.Count(row => row.ComplianceStatus != "Compliant"),
            rows);

        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, "ComplianceMatrixViewed", "system", "Project", project.Id, $"Compliance matrix with {matrix.TotalRows} rows and {matrix.OpenIssueCount} open issues was generated."),
            cancellationToken);

        return matrix;
    }

    private static IReadOnlyCollection<ComplianceMatrixRowResponse> BuildRows(KnowledgeProject project, bool hasExportApproval)
    {
        var rows = project.KnowledgeItems
            .OrderBy(item => item.SourceDocumentId is null)
            .ThenBy(item => SourceName(project, item))
            .ThenBy(item => item.Type)
            .ThenBy(item => item.Title)
            .Select(item => ToKnowledgeRow(project, item, hasExportApproval))
            .ToList();

        var documentIdsWithKnowledge = project.KnowledgeItems
            .Where(item => item.SourceDocumentId is not null)
            .Select(item => item.SourceDocumentId!.Value)
            .ToHashSet();

        rows.AddRange(project.Documents
            .Where(document => !documentIdsWithKnowledge.Contains(document.Id))
            .OrderBy(document => document.FileName)
            .Select(document => new ComplianceMatrixRowResponse(
                document.Id,
                document.FileName,
                KnowledgeItemId: null,
                KnowledgeTitle: "Kein Wissenselement",
                EvidenceType: "DocumentCoverage",
                ReviewStatus: document.Status.ToString(),
                QualityStatus: "-",
                ReviewHistoryCount: 0,
                ExportReady: false,
                ComplianceStatus: "OpenIssue",
                Gap: "Dokument hat noch kein extrahiertes oder verknuepftes Wissenselement.")));

        return rows.ToArray();
    }

    private static ComplianceMatrixRowResponse ToKnowledgeRow(KnowledgeProject project, KnowledgeItem item, bool hasExportApproval)
    {
        var source = SourceName(project, item);
        var exportReady = KnowledgeQualityPolicy.IsFinal(item) && hasExportApproval;
        var status = GetStatus(item, source, exportReady);

        return new ComplianceMatrixRowResponse(
            item.SourceDocumentId,
            source,
            item.Id,
            item.Title,
            item.Type.ToString(),
            item.ReviewStatus.ToString(),
            item.ExtractionQuality,
            item.ReviewHistory.Count,
            exportReady,
            status,
            GetGap(item, source, exportReady, status, hasExportApproval));
    }

    private static string SourceName(KnowledgeProject project, KnowledgeItem item)
    {
        if (item.SourceDocumentId is null)
        {
            return "Starterwissen";
        }

        return project.Documents.FirstOrDefault(document => document.Id == item.SourceDocumentId)?.FileName
            ?? item.SourceDocumentId.Value.ToString();
    }

    private static string GetStatus(KnowledgeItem item, string source, bool exportReady)
    {
        if (source == "Starterwissen")
        {
            return "OpenIssue";
        }

        if (!exportReady)
        {
            return "OpenIssue";
        }

        return item.ReviewHistory.Count == 0 ? "OpenIssue" : "Compliant";
    }

    private static string GetGap(KnowledgeItem item, string source, bool exportReady, string status, bool hasExportApproval)
    {
        if (status == "Compliant")
        {
            return "Nachweis vollstaendig.";
        }

        if (source == "Starterwissen")
        {
            return "Wissenselement braucht eine belegbare Quelle.";
        }

        if (!KnowledgeQualityPolicy.IsFinal(item))
        {
            return "Wissenselement ist nicht freigegeben und Verified.";
        }

        if (!hasExportApproval)
        {
            return "Export wurde noch nicht explizit freigegeben.";
        }

        return item.ReviewHistory.Count == 0
            ? "Review-Historie fehlt."
            : "Nachweis unvollstaendig.";
    }
}
