namespace AiKnowledgeTransfer.Application.Projects;

using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;

public static class ProjectMapper
{
    public static ProjectDetailsResponse ToDetails(KnowledgeProject project)
    {
        return new ProjectDetailsResponse(
            project.Id,
            project.Name,
            project.Description,
            project.Owner,
            project.CreatedAt,
            project.Documents.Select(ToResponse).ToArray(),
            project.Documents.SelectMany(document => document.Chunks.Select(chunk => ToResponse(document.Id, chunk))).ToArray(),
            project.KnowledgeItems.Select(ToResponse).ToArray(),
            project.Roadmaps.Select(RoadmapMapper.ToResponse).ToArray());
    }

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
            document.StoragePath,
            document.Status.ToString(),
            document.Chunks.Count,
            document.UploadedAt);
    }

    public static DocumentChunkResponse ToResponse(DocumentChunk chunk)
    {
        return ToResponse(Guid.Empty, chunk);
    }

    public static DocumentChunkResponse ToResponse(Guid documentId, DocumentChunk chunk)
    {
        return new DocumentChunkResponse(
            chunk.Id,
            documentId,
            chunk.ChunkNumber,
            chunk.Text,
            chunk.StartCharacter,
            chunk.EndCharacter,
            chunk.CreatedAt);
    }

    public static KnowledgeItemResponse ToResponse(KnowledgeItem item)
    {
        return new KnowledgeItemResponse(
            item.Id,
            item.Type.ToString(),
            item.Title,
            item.Summary,
            item.SourceDocumentId,
            item.SourceChunkNumber,
            item.ExtractionProvider,
            item.ExtractionModel,
            item.ExtractionUsedFallback,
            item.ExtractionQuality,
            item.ReviewStatus.ToString(),
            item.ReviewedBy,
            item.ReviewComment,
            item.ReviewedAt,
            item.CreatedAt);
    }
}
