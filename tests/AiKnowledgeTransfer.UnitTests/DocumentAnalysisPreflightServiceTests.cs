namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Infrastructure.Persistence;

public sealed class DocumentAnalysisPreflightServiceTests
{
    [Fact]
    public async Task GetAsync_reports_ready_for_small_supported_document()
    {
        var repository = new InMemoryProjectRepository();
        var project = await CreateProjectAsync(repository);
        var document = await RegisterDocumentAsync(repository, project.Id, "manual.md", "text/markdown", sizeInBytes: 1_000);
        var service = CreateService(repository, [new StubDocumentParser()]);

        var preflight = await service.GetAsync(project.Id, document.Id, CancellationToken.None);

        Assert.NotNull(preflight);
        Assert.Equal("Ready", preflight.Status);
        Assert.Equal("Small", preflight.WorkloadClass);
        Assert.True(preflight.ParserAvailable);
        Assert.Equal("Stub", preflight.ParserName);
        Assert.Contains(preflight.Recommendations, recommendation =>
            recommendation.Contains("within configured analysis", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAsync_recommends_review_for_document_close_to_extraction_limit()
    {
        var repository = new InMemoryProjectRepository();
        var project = await CreateProjectAsync(repository);
        var document = await RegisterDocumentAsync(repository, project.Id, "large.md", "text/markdown", sizeInBytes: 90_000);
        var service = CreateService(
            repository,
            [new StubDocumentParser()],
            analysisOptions: new DocumentAnalysisOptions(250, 500_000),
            extractionOptions: new KnowledgeExtractionOptions(80, 100_000));

        var preflight = await service.GetAsync(project.Id, document.Id, CancellationToken.None);

        Assert.NotNull(preflight);
        Assert.Equal("ReviewRecommended", preflight.Status);
        Assert.Equal("Large", preflight.WorkloadClass);
        Assert.Equal(90.0m, preflight.ExtractionLimitUtilizationPercent);
        Assert.Contains(preflight.Recommendations, recommendation =>
            recommendation.Contains("expensive for provider extraction", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAsync_blocks_when_no_parser_is_available()
    {
        var repository = new InMemoryProjectRepository();
        var project = await CreateProjectAsync(repository);
        var document = await RegisterDocumentAsync(repository, project.Id, "manual.pdf", "application/pdf", sizeInBytes: 1_000);
        var service = CreateService(repository, []);

        var preflight = await service.GetAsync(project.Id, document.Id, CancellationToken.None);

        Assert.NotNull(preflight);
        Assert.Equal("Blocked", preflight.Status);
        Assert.False(preflight.ParserAvailable);
        Assert.Equal("None", preflight.ParserName);
        Assert.Contains(preflight.Recommendations, recommendation =>
            recommendation.Contains("No parser is registered", StringComparison.Ordinal));
    }

    private static DocumentAnalysisPreflightService CreateService(
        InMemoryProjectRepository repository,
        IReadOnlyCollection<IDocumentParser> parsers,
        DocumentAnalysisOptions? analysisOptions = null,
        KnowledgeExtractionOptions? extractionOptions = null)
    {
        return new DocumentAnalysisPreflightService(
            repository,
            parsers,
            analysisOptions ?? new DocumentAnalysisOptions(250, 500_000),
            extractionOptions ?? new KnowledgeExtractionOptions(80, 120_000));
    }

    private static async Task<ProjectSummaryResponse> CreateProjectAsync(InMemoryProjectRepository repository)
    {
        var service = new ProjectService(repository, new UnusedFileStorage());
        return await service.CreateAsync(
            new CreateProjectRequest("Preflight Project", "Large document performance test", "Engineering"),
            CancellationToken.None);
    }

    private static async Task<DocumentResponse> RegisterDocumentAsync(
        InMemoryProjectRepository repository,
        Guid projectId,
        string fileName,
        string contentType,
        long sizeInBytes)
    {
        var service = new ProjectService(repository, new UnusedFileStorage());
        var document = await service.RegisterDocumentAsync(
            projectId,
            new RegisterDocumentRequest(fileName, contentType, "Manual upload", sizeInBytes),
            CancellationToken.None);

        return document!;
    }

    private sealed class StubDocumentParser : IDocumentParser
    {
        public string Name => "Stub";

        public IReadOnlyCollection<string> SupportedContentTypes => ["text/markdown"];

        public IReadOnlyCollection<string> SupportedFileExtensions => [".md"];

        public string CapabilityDescription => "Stub parser for preflight tests.";

        public bool CanParse(string contentType, string fileName)
        {
            return fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ParsedDocument> ParseAsync(
            string contentType,
            string fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Preflight does not parse documents.");
        }
    }

    private sealed class UnusedFileStorage : IFileStorage
    {
        public Task<StoredFile> SaveAsync(
            Guid projectId,
            string fileName,
            string contentType,
            Stream content,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Preflight tests register metadata only.");
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Preflight tests do not open files.");
        }
    }
}
