namespace AiKnowledgeTransfer.Application.Documents;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Documents;

public sealed class DocumentAnalysisService(
    IProjectRepository projects,
    IFileStorage fileStorage,
    IEnumerable<IDocumentParser> parsers)
{
    public async Task<AnalyzeDocumentResponse?> AnalyzeAsync(Guid projectId, Guid documentId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var document = project?.Documents.FirstOrDefault(candidate => candidate.Id == documentId);
        if (document is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(document.StoragePath))
        {
            throw new InvalidOperationException("Document has no stored file to analyze.");
        }

        var parser = parsers.FirstOrDefault(candidate => candidate.CanParse(document.ContentType, document.FileName));
        if (parser is null)
        {
            throw new NotSupportedException($"No parser is registered for '{document.ContentType}' files.");
        }

        await using var content = await fileStorage.OpenReadAsync(document.StoragePath, cancellationToken);
        var parsedDocument = await parser.ParseAsync(
            document.ContentType,
            document.FileName,
            content,
            cancellationToken);

        document.ReplaceChunks(parsedDocument.Chunks.Select(ToDomainChunk));

        await projects.SaveChangesAsync(cancellationToken);

        return new AnalyzeDocumentResponse(
            document.Id,
            document.Status.ToString(),
            document.Chunks.Count,
            document.Chunks.Select(ProjectMapper.ToResponse).ToArray());
    }

    private static DocumentChunk ToDomainChunk(ParsedDocumentChunk chunk)
    {
        return new DocumentChunk(
            chunk.ChunkNumber,
            chunk.Text,
            chunk.StartCharacter,
            chunk.EndCharacter);
    }
}
