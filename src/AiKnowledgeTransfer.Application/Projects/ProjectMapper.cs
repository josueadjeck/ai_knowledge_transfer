namespace AiKnowledgeTransfer.Application.Projects;

using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Projects;

internal static class ProjectMapper
{
    public static ProjectSummaryResponse ToSummary(KnowledgeProject project)
    {
        return new ProjectSummaryResponse(
            project.Id,
            project.Name,
            project.Description,
            project.Owner,
            project.CreatedAt,
            project.Documents.Count,
            project.KnowledgeItems.Count,
            project.Roadmaps.Count);
    }

    public static DocumentResponse ToResponse(DocumentVersion document)
    {
        return new DocumentResponse(
            document.Id,
            document.FileName,
            document.ContentType,
            document.Source,
            document.SizeInBytes,
            document.VersionNumber,
            document.Status.ToString(),
            document.UploadedAt);
    }
}
