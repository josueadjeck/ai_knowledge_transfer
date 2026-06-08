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

    [Fact]
    public async Task ParseAsync_reports_ocr_requirement_for_pdf_without_text()
    {
        var parser = new PdfDocumentParser();
        await using var stream = new MemoryStream(CreateEmptyPdf());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAsync(
            "application/pdf",
            "scanned.pdf",
            stream,
            CancellationToken.None));

        Assert.Contains("OCR", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreateEmptyPdf()
    {
        return "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Count 0 >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF"u8.ToArray();
    }
}
