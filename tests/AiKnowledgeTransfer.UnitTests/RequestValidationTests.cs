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
}
