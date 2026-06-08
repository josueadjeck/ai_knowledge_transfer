using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Infrastructure.Parsing;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class DocumentParserCapabilityServiceTests
{
    [Fact]
    public void List_returns_supported_formats_for_registered_parsers()
    {
        var service = new DocumentParserCapabilityService(
        [
            new PlainTextDocumentParser(),
            new PdfDocumentParser(),
            new WordDocumentParser()
        ]);

        var capabilities = service.List();

        Assert.Contains(capabilities, capability =>
            capability.ParserName == nameof(PlainTextDocumentParser)
            && capability.ContentTypes.Contains("text/*")
            && capability.FileExtensions.Contains(".md"));
        Assert.Contains(capabilities, capability =>
            capability.ParserName == nameof(PdfDocumentParser)
            && capability.ContentTypes.Contains("application/pdf")
            && capability.FileExtensions.Contains(".pdf")
            && capability.Detail.Contains("OCR", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(capabilities, capability =>
            capability.ParserName == nameof(WordDocumentParser)
            && capability.FileExtensions.Contains(".docx"));
    }
}
