using AiKnowledgeTransfer.Infrastructure.Parsing;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class PdfDocumentParserTests
{
    [Theory]
    [InlineData("application/pdf", "manual.bin")]
    [InlineData("application/octet-stream", "manual.pdf")]
    public void CanParse_accepts_pdf_content_type_or_extension(string contentType, string fileName)
    {
        var parser = new PdfDocumentParser();

        Assert.True(parser.CanParse(contentType, fileName));
    }

    [Fact]
    public void CanParse_rejects_plain_text_documents()
    {
        var parser = new PdfDocumentParser();

        Assert.False(parser.CanParse("text/plain", "manual.txt"));
    }
}
