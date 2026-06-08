using AiKnowledgeTransfer.Infrastructure.Parsing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class WordDocumentParserTests
{
    [Theory]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "manual.bin")]
    [InlineData("application/octet-stream", "manual.docx")]
    public void CanParse_accepts_docx_content_type_or_extension(string contentType, string fileName)
    {
        var parser = new WordDocumentParser();

        Assert.True(parser.CanParse(contentType, fileName));
    }

    [Fact]
    public async Task ParseAsync_extracts_paragraphs_from_docx()
    {
        var parser = new WordDocumentParser();
        await using var stream = CreateDocxStream("System overview", "Deployment workflow must be reviewed.");

        var parsed = await parser.ParseAsync(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "manual.docx",
            stream,
            CancellationToken.None);

        Assert.Equal(nameof(WordDocumentParser), parsed.ParserName);
        Assert.Contains("Word parser created", parsed.Detail, StringComparison.Ordinal);
        Assert.Equal(2, parsed.Chunks.Count);
        Assert.All(parsed.Chunks, chunk => Assert.Contains("Word document body", chunk.SourceReference, StringComparison.Ordinal));
        Assert.All(parsed.Chunks, chunk => Assert.Contains("Text", chunk.QualityStatus, StringComparison.Ordinal));
        Assert.Contains(parsed.Chunks, chunk => chunk.Text == "System overview");
        Assert.Contains(parsed.Chunks, chunk => chunk.Text.Contains("Deployment", StringComparison.Ordinal));
    }

    private static MemoryStream CreateDocxStream(params string[] paragraphs)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            foreach (var paragraph in paragraphs)
            {
                body.AppendChild(new Paragraph(new Run(new Text(paragraph))));
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }
}
