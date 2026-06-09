namespace AiKnowledgeTransfer.Application.Knowledge;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;

public sealed class KnowledgeExtractionService(
    IProjectRepository projects,
    IKnowledgeExtractor extractor,
    IAuditLog? auditLog = null,
    KnowledgeExtractionOptions? options = null)
{
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;
    private readonly KnowledgeExtractionOptions _options = options ?? KnowledgeExtractionOptions.Default;

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

        ValidateExtractionInput(document.Id, document.Chunks);
        var extraction = await extractor.ExtractAsync(document.Chunks, cancellationToken);
        var createdItems = extraction.Items
            .Select(item => project.AddKnowledgeItem(
                item.Type,
                item.Title,
                item.Summary,
                document.Id,
                item.SourceChunkNumber,
                extraction.ProviderName,
                extraction.ProviderModel,
                extraction.UsedFallback,
                DetermineQuality(extraction, item)))
            .ToArray();

        await projects.SaveChangesAsync(cancellationToken);
        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, "KnowledgeExtracted", "system", "Document", document.Id, $"{createdItems.Length} knowledge items were extracted."),
            cancellationToken);

        return new ExtractKnowledgeResponse(
            document.Id,
            createdItems.Length,
            extraction.ProviderName,
            extraction.UsedFallback,
            extraction.Detail,
            createdItems.Select(ProjectMapper.ToResponse).ToArray());
    }

    private static string DetermineQuality(KnowledgeExtractionResult extraction, ExtractedKnowledgeItem item)
    {
        if (item.Type == Domain.Knowledge.KnowledgeItemType.OpenQuestion)
        {
            return "Uncertain";
        }

        return extraction.UsedFallback ? "FallbackReview" : "ProviderSuggested";
    }

    private void ValidateExtractionInput(Guid documentId, IReadOnlyCollection<Domain.Documents.DocumentChunk> chunks)
    {
        if (chunks.Count > _options.MaxChunksPerExtraction)
        {
            throw new KnowledgeExtractionLimitExceededException(
                documentId,
                "chunks",
                chunks.Count,
                _options.MaxChunksPerExtraction);
        }

        var totalChunkCharacters = chunks.Sum(chunk => chunk.Text.Length);
        if (totalChunkCharacters > _options.MaxTotalChunkCharacters)
        {
            throw new KnowledgeExtractionLimitExceededException(
                documentId,
                "chunk characters",
                totalChunkCharacters,
                _options.MaxTotalChunkCharacters);
        }
    }
}
