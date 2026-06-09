namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Storage;

public sealed class KnowledgeExtractionServiceTests
{
    [Fact]
    public async Task ExtractAsync_rejects_documents_that_exceed_chunk_limit_before_provider_call()
    {
        var repository = new InMemoryProjectRepository();
        var extractor = new CountingExtractor();
        var documentId = await CreateAnalyzedDocumentAsync(
            repository,
            [CreateChunk(1, "Chunk one"), CreateChunk(2, "Chunk two"), CreateChunk(3, "Chunk three")]);
        var project = await repository.ListAsync(CancellationToken.None);
        var service = new KnowledgeExtractionService(
            repository,
            extractor,
            options: new KnowledgeExtractionOptions(2, 1_000));

        var exception = await Assert.ThrowsAsync<KnowledgeExtractionLimitExceededException>(() =>
            service.ExtractAsync(project.Single().Id, documentId, CancellationToken.None));

        Assert.Equal("chunks", exception.LimitName);
        Assert.Equal(3, exception.ActualValue);
        Assert.Equal(2, exception.ConfiguredLimit);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task ExtractAsync_rejects_documents_that_exceed_character_limit_before_provider_call()
    {
        var repository = new InMemoryProjectRepository();
        var extractor = new CountingExtractor();
        var documentId = await CreateAnalyzedDocumentAsync(
            repository,
            [CreateChunk(1, new string('A', 20)), CreateChunk(2, new string('B', 20))]);
        var project = await repository.ListAsync(CancellationToken.None);
        var service = new KnowledgeExtractionService(
            repository,
            extractor,
            options: new KnowledgeExtractionOptions(10, 30));

        var exception = await Assert.ThrowsAsync<KnowledgeExtractionLimitExceededException>(() =>
            service.ExtractAsync(project.Single().Id, documentId, CancellationToken.None));

        Assert.Equal("chunk characters", exception.LimitName);
        Assert.Equal(40, exception.ActualValue);
        Assert.Equal(30, exception.ConfiguredLimit);
        Assert.Equal(0, extractor.CallCount);
    }

    private static async Task<Guid> CreateAnalyzedDocumentAsync(
        InMemoryProjectRepository repository,
        IReadOnlyCollection<DocumentChunk> chunks)
    {
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var projectService = new ProjectService(repository, storage);
        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Extraction Limit Project", "Provider guardrail test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("Document body"u8.ToArray());
        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand(
                "knowledge.md",
                "text/markdown",
                "Manual upload",
                content),
            CancellationToken.None);

        var storedProject = await repository.GetAsync(project.Id, CancellationToken.None);
        var document = storedProject!.Documents.Single(document => document.Id == upload!.Document.Id);
        document.ReplaceChunks(chunks);
        await repository.SaveChangesAsync(CancellationToken.None);

        return document.Id;
    }

    private static DocumentChunk CreateChunk(int chunkNumber, string text)
    {
        return new DocumentChunk(
            chunkNumber,
            text,
            0,
            text.Length,
            "Test chunk",
            "UsableText");
    }

    private sealed class CountingExtractor : IKnowledgeExtractor
    {
        public int CallCount { get; private set; }

        public Task<KnowledgeExtractionResult> ExtractAsync(
            IReadOnlyCollection<DocumentChunk> chunks,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new KnowledgeExtractionResult([], "Stub", null, false, "Stub"));
        }
    }
}
