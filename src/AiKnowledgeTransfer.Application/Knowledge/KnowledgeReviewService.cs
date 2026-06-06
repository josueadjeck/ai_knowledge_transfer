namespace AiKnowledgeTransfer.Application.Knowledge;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Knowledge;

public sealed class KnowledgeReviewService(IProjectRepository projects)
{
    public Task<KnowledgeItemResponse?> SubmitForReviewAsync(
        Guid projectId,
        Guid knowledgeItemId,
        ReviewKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateReviewStatusAsync(
            projectId,
            knowledgeItemId,
            item => item.SubmitForReview(request.Reviewer, request.Comment),
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
            item => item.Approve(request.Reviewer, request.Comment),
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
            item => item.Reject(request.Reviewer, request.Comment),
            cancellationToken);
    }

    private async Task<KnowledgeItemResponse?> UpdateReviewStatusAsync(
        Guid projectId,
        Guid knowledgeItemId,
        Action<KnowledgeItem> update,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var item = project?.KnowledgeItems.FirstOrDefault(candidate => candidate.Id == knowledgeItemId);
        if (item is null)
        {
            return null;
        }

        update(item);
        await projects.SaveChangesAsync(cancellationToken);

        return ProjectMapper.ToResponse(item);
    }
}
