namespace AiKnowledgeTransfer.Application.Documents;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Validation;
using AiKnowledgeTransfer.Domain.Documents;

public sealed class DocumentAnalysisService(
    IProjectRepository projects,
    IFileStorage fileStorage,
    IEnumerable<IDocumentParser> parsers,
    IAuditLog? auditLog = null,
    DocumentAnalysisOptions? options = null)
{
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;
    private readonly DocumentAnalysisOptions _options = options ?? DocumentAnalysisOptions.Default;

    public async Task<AnalyzeDocumentResponse?> AnalyzeAsync(Guid projectId, Guid documentId, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var document = project?.Documents.FirstOrDefault(candidate => candidate.Id == documentId);
        if (project is null || document is null)
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
            throw new NotSupportedException($"No parser is registered for '{document.ContentType}' files. Supported types: {DocumentFileValidation.SupportedFileTypesDescription}.");
        }

        await using var content = await fileStorage.OpenReadAsync(document.StoragePath, cancellationToken);
        var parsedDocument = await parser.ParseAsync(
            document.ContentType,
            document.FileName,
            content,
            cancellationToken);

        ValidateParsedDocument(parsedDocument, document.FileName);
        document.ReplaceChunks(parsedDocument.Chunks.Select(ToDomainChunk));

        await projects.SaveChangesAsync(cancellationToken);
        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, "DocumentAnalyzed", "system", "Document", document.Id, $"Document '{document.FileName}' was analyzed by {parsedDocument.ParserName} into {document.Chunks.Count} chunks."),
            cancellationToken);

        return new AnalyzeDocumentResponse(
            document.Id,
            document.Status.ToString(),
            document.Chunks.Count,
            parsedDocument.ParserName,
            parsedDocument.Detail,
            document.Chunks
                .GroupBy(chunk => chunk.QualityStatus, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new DocumentChunkQualitySummaryResponse(group.Key, group.Count()))
                .ToArray(),
            document.Chunks.Select(chunk => ProjectMapper.ToResponse(document.Id, chunk)).ToArray());
    }

    private static DocumentChunk ToDomainChunk(ParsedDocumentChunk chunk)
    {
        return new DocumentChunk(
            chunk.ChunkNumber,
            chunk.Text,
            chunk.StartCharacter,
            chunk.EndCharacter,
            chunk.SourceReference,
            chunk.QualityStatus);
    }

    private void ValidateParsedDocument(ParsedDocument parsedDocument, string fileName)
    {
        if (parsedDocument.Chunks.Count > _options.MaxChunksPerDocument)
        {
            throw new DocumentAnalysisLimitExceededException(
                fileName,
                "chunks",
                parsedDocument.Chunks.Count,
                _options.MaxChunksPerDocument);
        }

        var totalExtractedCharacters = parsedDocument.Chunks.Sum(chunk => chunk.Text.Length);
        if (totalExtractedCharacters > _options.MaxTotalExtractedCharacters)
        {
            throw new DocumentAnalysisLimitExceededException(
                fileName,
                "extracted characters",
                totalExtractedCharacters,
                _options.MaxTotalExtractedCharacters);
        }
    }
}
