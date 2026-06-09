namespace AiKnowledgeTransfer.Application.Documents;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Documents;

public sealed class DocumentVersionComparisonService(IProjectRepository projects)
{
    public async Task<DocumentVersionComparisonResponse?> CompareAsync(
        Guid projectId,
        Guid baseDocumentId,
        Guid targetDocumentId,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var baseDocument = project.Documents.FirstOrDefault(document => document.Id == baseDocumentId);
        var targetDocument = project.Documents.FirstOrDefault(document => document.Id == targetDocumentId);
        if (baseDocument is null || targetDocument is null)
        {
            return null;
        }

        if (!baseDocument.FileName.Equals(targetDocument.FileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only versions of the same file can be compared.");
        }

        var rows = BuildRows(baseDocument, targetDocument);

        return new DocumentVersionComparisonResponse(
            project.Id,
            baseDocument.Id,
            targetDocument.Id,
            baseDocument.FileName,
            baseDocument.VersionNumber,
            targetDocument.VersionNumber,
            baseDocument.Chunks.Count,
            targetDocument.Chunks.Count,
            rows.Count(row => row.ChangeType == "Added"),
            rows.Count(row => row.ChangeType == "Removed"),
            rows.Count(row => row.ChangeType == "Changed"),
            rows.Count(row => row.ChangeType == "Unchanged"),
            rows);
    }

    private static IReadOnlyCollection<DocumentVersionComparisonRowResponse> BuildRows(
        DocumentVersion baseDocument,
        DocumentVersion targetDocument)
    {
        var baseChunks = baseDocument.Chunks.OrderBy(chunk => chunk.ChunkNumber).ToArray();
        var targetChunks = targetDocument.Chunks.OrderBy(chunk => chunk.ChunkNumber).ToArray();
        var maxRows = Math.Max(baseChunks.Length, targetChunks.Length);
        var rows = new List<DocumentVersionComparisonRowResponse>(maxRows);

        for (var index = 0; index < maxRows; index++)
        {
            var baseChunk = index < baseChunks.Length ? baseChunks[index] : null;
            var targetChunk = index < targetChunks.Length ? targetChunks[index] : null;
            var changeType = GetChangeType(baseChunk, targetChunk);

            rows.Add(new DocumentVersionComparisonRowResponse(
                index + 1,
                changeType,
                baseChunk?.ChunkNumber,
                targetChunk?.ChunkNumber,
                baseChunk?.Text ?? string.Empty,
                targetChunk?.Text ?? string.Empty,
                baseChunk?.QualityStatus ?? string.Empty,
                targetChunk?.QualityStatus ?? string.Empty));
        }

        return rows;
    }

    private static string GetChangeType(DocumentChunk? baseChunk, DocumentChunk? targetChunk)
    {
        if (baseChunk is null)
        {
            return "Added";
        }

        if (targetChunk is null)
        {
            return "Removed";
        }

        return Normalize(baseChunk.Text).Equals(Normalize(targetChunk.Text), StringComparison.Ordinal)
            ? "Unchanged"
            : "Changed";
    }

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
