namespace AiKnowledgeTransfer.Application.Knowledge;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Knowledge;

public sealed class KnowledgeReviewService(
    IProjectRepository projects,
    IAuditLog? auditLog = null)
{
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;

    public Task<KnowledgeItemResponse?> SubmitForReviewAsync(
        Guid projectId,
        Guid knowledgeItemId,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateReviewStatusAsync(
            projectId,
            knowledgeItemId,
            item =>
            {
                item.SubmitForReview(request.Reviewer, request.Comment);
                ApplyQualityStatus(item, request.QualityStatus);
            },
            "KnowledgeReviewSubmitted",
            request.Reviewer,
            cancellationToken);
    }

    public Task<KnowledgeItemResponse?> ApproveAsync(
        Guid projectId,
        Guid knowledgeItemId,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateReviewStatusAsync(
            projectId,
            knowledgeItemId,
            item =>
            {
                item.Approve(request.Reviewer, request.Comment);
                ApplyQualityStatus(item, request.QualityStatus);
            },
            "KnowledgeApproved",
            request.Reviewer,
            cancellationToken);
    }

    public Task<KnowledgeItemResponse?> RejectAsync(
        Guid projectId,
        Guid knowledgeItemId,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateReviewStatusAsync(
            projectId,
            knowledgeItemId,
            item =>
            {
                item.Reject(request.Reviewer, request.Comment);
                ApplyQualityStatus(item, request.QualityStatus);
            },
            "KnowledgeRejected",
            request.Reviewer,
            cancellationToken);
    }

    private async Task<KnowledgeItemResponse?> UpdateReviewStatusAsync(
        Guid projectId,
        Guid knowledgeItemId,
        Action<KnowledgeItem> update,
        string action,
        string actor,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var item = project?.KnowledgeItems.FirstOrDefault(candidate => candidate.Id == knowledgeItemId);
        if (project is null || item is null)
        {
            return null;
        }

        update(item);
        await projects.SaveChangesAsync(cancellationToken);
        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, action, actor, "KnowledgeItem", item.Id, $"Knowledge item '{item.Title}' changed to {item.ReviewStatus} with quality {item.ExtractionQuality}."),
            cancellationToken);

        return ProjectMapper.ToResponse(item);
    }

    private static void ApplyQualityStatus(KnowledgeItem item, string? qualityStatus)
    {
        if (!string.IsNullOrWhiteSpace(qualityStatus))
        {
            item.UpdateExtractionQuality(qualityStatus);
        }
    }
}
