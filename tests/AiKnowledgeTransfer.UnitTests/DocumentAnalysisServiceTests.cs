namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Infrastructure.Persistence;
using AiKnowledgeTransfer.Infrastructure.Storage;

public sealed class DocumentAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_rejects_documents_that_exceed_chunk_limit()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var parser = new StubDocumentParser(CreateChunks(3, "Small chunk body"));
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(
            repository,
            storage,
            [parser],
            options: new DocumentAnalysisOptions(2, 1_000));

        var upload = await UploadTextDocumentAsync(projectService);

        var exception = await Assert.ThrowsAsync<DocumentAnalysisLimitExceededException>(() =>
            analysisService.AnalyzeAsync(upload.ProjectId, upload.DocumentId, CancellationToken.None));

        Assert.Contains("configured analysis limit is 2 chunks", exception.Message);
        Assert.Equal("chunks", exception.LimitName);
        Assert.Equal(3, exception.ActualValue);
        Assert.Equal(2, exception.ConfiguredLimit);

        var project = await repository.GetAsync(upload.ProjectId, CancellationToken.None);
        Assert.Empty(project!.Documents.Single().Chunks);
    }

    [Fact]
    public async Task AnalyzeAsync_rejects_documents_that_exceed_total_character_limit()
    {
        var repository = new InMemoryProjectRepository();
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-tests", Guid.NewGuid().ToString("N")));
        var parser = new StubDocumentParser(CreateChunks(2, new string('A', 20)));
        var projectService = new ProjectService(repository, storage);
        var analysisService = new DocumentAnalysisService(
            repository,
            storage,
            [parser],
            options: new DocumentAnalysisOptions(10, 30));

        var upload = await UploadTextDocumentAsync(projectService);

        var exception = await Assert.ThrowsAsync<DocumentAnalysisLimitExceededException>(() =>
            analysisService.AnalyzeAsync(upload.ProjectId, upload.DocumentId, CancellationToken.None));

        Assert.Contains("configured analysis limit is 30 extracted characters", exception.Message);
        Assert.Equal("extracted characters", exception.LimitName);
        Assert.Equal(40, exception.ActualValue);
        Assert.Equal(30, exception.ConfiguredLimit);

        var project = await repository.GetAsync(upload.ProjectId, CancellationToken.None);
        Assert.Empty(project!.Documents.Single().Chunks);
    }

    private static async Task<(Guid ProjectId, Guid DocumentId)> UploadTextDocumentAsync(ProjectService projectService)
    {
        var project = await projectService.CreateAsync(
            new CreateProjectRequest("Analysis Limit Project", "Parser guardrail test", "Engineering"),
            CancellationToken.None);

        await using var content = new MemoryStream("Document body"u8.ToArray());
        var upload = await projectService.UploadDocumentAsync(
            project.Id,
            new UploadDocumentCommand(
                "analysis.md",
                "text/markdown",
                "Manual upload",
                content),
            CancellationToken.None);

        return (project.Id, upload!.Document.Id);
    }

    private static IReadOnlyCollection<ParsedDocumentChunk> CreateChunks(int count, string text)
    {
        return Enumerable.Range(1, count)
            .Select(chunkNumber => new ParsedDocumentChunk(
                chunkNumber,
                text,
                (chunkNumber - 1) * text.Length,
                chunkNumber * text.Length,
                "Stub parser"))
            .ToArray();
    }

    private sealed class StubDocumentParser(IReadOnlyCollection<ParsedDocumentChunk> chunks) : IDocumentParser
    {
        public string Name => "Stub";

        public IReadOnlyCollection<string> SupportedContentTypes => ["text/markdown"];

        public IReadOnlyCollection<string> SupportedFileExtensions => [".md"];

        public string CapabilityDescription => "Stub parser for analysis limit tests.";

        public bool CanParse(string contentType, string fileName)
        {
            return true;
        }

        public Task<ParsedDocument> ParseAsync(
            string contentType,
            string fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ParsedDocument(chunks, Name, "Stub parser result."));
        }
    }
}
