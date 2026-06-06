namespace AiKnowledgeTransfer.Application.Knowledge;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;

public sealed class KnowledgeExtractionService(
    IProjectRepository projects,
    IKnowledgeExtractor extractor)
{
    public async Task<ExtractKnowledgeResponse?> ExtractAsync(Guid projectId, Guid documentId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var document = project?.Documents.FirstOrDefault(candidate => candidate.Id == documentId);
        if (project is null || document is null)
        {
            return null;
        }

        if (document.Chunks.Count == 0)
        {
            throw new InvalidOperationException("Document must be analyzed before knowledge can be extracted.");
        }

        var extraction = await extractor.ExtractAsync(document.Chunks, cancellationToken);
        var createdItems = extraction.Items
            .Select(item => project.AddKnowledgeItem(item.Type, item.Title, item.Summary, document.Id))
            .ToArray();

        await projects.SaveChangesAsync(cancellationToken);

        return new ExtractKnowledgeResponse(
            document.Id,
            createdItems.Length,
            createdItems.Select(ProjectMapper.ToResponse).ToArray());
    }
}
