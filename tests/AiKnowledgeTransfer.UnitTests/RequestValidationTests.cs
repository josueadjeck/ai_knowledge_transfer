using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Contracts.Validation;

namespace AiKnowledgeTransfer.UnitTests;

public sealed class RequestValidationTests
{
    [Fact]
    public void Validate_create_project_requires_core_fields()
    {
        var errors = RequestValidation.Validate(new CreateProjectRequest("", " ", ""));

        Assert.Contains(nameof(CreateProjectRequest.Name), errors.Keys);
        Assert.Contains(nameof(CreateProjectRequest.Description), errors.Keys);
        Assert.Contains(nameof(CreateProjectRequest.Owner), errors.Keys);
    }

    [Fact]
    public void Validate_register_document_requires_positive_size()
    {
        var errors = RequestValidation.Validate(new RegisterDocumentRequest("manual.md", "text/markdown", "Repository", 0));

        Assert.Contains(nameof(RegisterDocumentRequest.SizeInBytes), errors.Keys);
    }

    [Fact]
    public void Validate_register_document_rejects_unsupported_file_type()
    {
        var errors = RequestValidation.Validate(new RegisterDocumentRequest("tool.exe", "application/octet-stream", "Repository", 128));

        Assert.Contains(nameof(RegisterDocumentRequest.ContentType), errors.Keys);
    }

    [Fact]
    public void Validate_register_document_rejects_files_above_upload_limit()
    {
        var errors = RequestValidation.Validate(new RegisterDocumentRequest(
            "large.pdf",
            "application/pdf",
            "Repository",
            DocumentFileValidation.MaxUploadSizeInBytes + 1));

        Assert.Contains(nameof(RegisterDocumentRequest.SizeInBytes), errors.Keys);
    }

    [Theory]
    [InlineData("manual.txt", "text/plain")]
    [InlineData("manual.md", "application/octet-stream")]
    [InlineData("manual.pdf", "application/pdf")]
    [InlineData("manual.docx", "application/octet-stream")]
    public void Document_file_validation_accepts_supported_types(string fileName, string contentType)
    {
        var errors = DocumentFileValidation.Validate(fileName, contentType, 128);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void Validate_roadmap_requires_duration_between_one_and_eight_weeks(int durationInWeeks)
    {
        var errors = RequestValidation.Validate(new GenerateRoadmapRequest("Support Engineer", durationInWeeks));

        Assert.Contains(nameof(GenerateRoadmapRequest.DurationInWeeks), errors.Keys);
    }

    [Fact]
    public void Validate_review_requires_reviewer_and_comment()
    {
        var errors = RequestValidation.Validate(new ReviewKnowledgeItemRequest("", ""));

        Assert.Contains(nameof(ReviewKnowledgeItemRequest.Reviewer), errors.Keys);
        Assert.Contains(nameof(ReviewKnowledgeItemRequest.Comment), errors.Keys);
    }

    [Fact]
    public void Validate_review_rejects_unknown_quality_status()
    {
        var errors = RequestValidation.Validate(new ReviewKnowledgeItemRequest("Reviewer", "Comment", "Guessed"));

        Assert.Contains(nameof(ReviewKnowledgeItemRequest.QualityStatus), errors.Keys);
    }

    [Fact]
    public void Validate_bulk_review_requires_item_ids()
    {
        var errors = RequestValidation.Validate(new BulkReviewKnowledgeItemsRequest([], "Reviewer", "Comment"));

        Assert.Contains(nameof(BulkReviewKnowledgeItemsRequest.KnowledgeItemIds), errors.Keys);
    }

    [Fact]
    public void Validate_bulk_review_rejects_unknown_quality_status()
    {
        var errors = RequestValidation.Validate(new BulkReviewKnowledgeItemsRequest([Guid.NewGuid()], "Reviewer", "Comment", "Guessed"));

        Assert.Contains(nameof(BulkReviewKnowledgeItemsRequest.QualityStatus), errors.Keys);
    }
}
