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
                ApplyQualityStatus(item, request.QualityStatus, KnowledgeQualityPolicy.NeedsClarification);
                item.RecordReviewHistory("SubmitForReview");
            },
            "KnowledgeReviewSubmitted",
            request.Reviewer,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<KnowledgeItemResponse>> SubmitManyForReviewAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> knowledgeItemIds,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateManyReviewStatusesAsync(
            projectId,
            knowledgeItemIds,
            item =>
            {
                item.SubmitForReview(request.Reviewer, request.Comment);
                ApplyQualityStatus(item, request.QualityStatus, KnowledgeQualityPolicy.NeedsClarification);
                item.RecordReviewHistory("SubmitForReview");
            },
            "KnowledgeReviewBulkSubmitted",
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
                ApplyQualityStatus(item, request.QualityStatus, KnowledgeQualityPolicy.Verified);
                item.RecordReviewHistory("Approve");
            },
            "KnowledgeApproved",
            request.Reviewer,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<KnowledgeItemResponse>> ApproveManyAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> knowledgeItemIds,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateManyReviewStatusesAsync(
            projectId,
            knowledgeItemIds,
            item =>
            {
                item.Approve(request.Reviewer, request.Comment);
                ApplyQualityStatus(item, request.QualityStatus, KnowledgeQualityPolicy.Verified);
                item.RecordReviewHistory("Approve");
            },
            "KnowledgeBulkApproved",
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
                ApplyQualityStatus(item, request.QualityStatus, KnowledgeQualityPolicy.RejectedSource);
                item.RecordReviewHistory("Reject");
            },
            "KnowledgeRejected",
            request.Reviewer,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<KnowledgeItemResponse>> RejectManyAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> knowledgeItemIds,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateManyReviewStatusesAsync(
            projectId,
            knowledgeItemIds,
            item =>
            {
                item.Reject(request.Reviewer, request.Comment);
                ApplyQualityStatus(item, request.QualityStatus, KnowledgeQualityPolicy.RejectedSource);
                item.RecordReviewHistory("Reject");
            },
            "KnowledgeBulkRejected",
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

    private async Task<IReadOnlyCollection<KnowledgeItemResponse>> UpdateManyReviewStatusesAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> knowledgeItemIds,
        Action<KnowledgeItem> update,
        string action,
        string actor,
        CancellationToken cancellationToken)
    {
        if (knowledgeItemIds.Count == 0)
        {
            return [];
        }

        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return [];
        }

        var requestedIds = knowledgeItemIds.ToHashSet();
        var items = project.KnowledgeItems
            .Where(item => requestedIds.Contains(item.Id))
            .ToArray();

        foreach (var item in items)
        {
            update(item);
        }

        await projects.SaveChangesAsync(cancellationToken);
        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, action, actor, "Project", project.Id, $"{items.Length} knowledge items were changed by bulk review action."),
            cancellationToken);

        return items.Select(ProjectMapper.ToResponse).ToArray();
    }

    private static void ApplyQualityStatus(KnowledgeItem item, string? qualityStatus, string defaultQualityStatus)
    {
        item.UpdateExtractionQuality(string.IsNullOrWhiteSpace(qualityStatus) ? defaultQualityStatus : qualityStatus);
    }
}
