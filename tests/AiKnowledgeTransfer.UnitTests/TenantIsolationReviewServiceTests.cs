namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Operations;
using AiKnowledgeTransfer.Application.Security;

public sealed class TenantIsolationReviewServiceTests
{
    [Fact]
    public void GetReview_accepts_single_tenant_as_mvp_mode()
    {
        var service = new TenantIsolationReviewService(new TenantOptions("SingleTenant", "tenant_id", "default"));

        var review = service.GetReview();

        Assert.Equal("SingleTenant", review.Status);
        Assert.Equal("SingleTenant", review.TenancyMode);
        Assert.Equal(0, review.ManualAreaCount);
        Assert.Contains(review.Areas, area => area.Area == "Configuration" && area.Status == "Pass");
        Assert.Contains(review.Areas, area => area.Area == "Persistence records" && area.Status == "Planned");
    }

    [Fact]
    public void GetReview_requires_manual_review_for_multi_tenant_mode()
    {
        var service = new TenantIsolationReviewService(new TenantOptions("MultiTenant", "tenant_id", "default"));

        var review = service.GetReview();

        Assert.Equal("NeedsReview", review.Status);
        Assert.True(review.ManualAreaCount > 0);
        Assert.Contains(review.Areas, area => area.Area == "API authorization" && area.Status == "Manual");
        Assert.Contains(review.Notes, note => note.Contains("not implemented end to end", StringComparison.Ordinal));
    }

    [Fact]
    public void GetReview_blocks_invalid_tenancy_configuration()
    {
        var service = new TenantIsolationReviewService(new TenantOptions("Shared", "tenant_id", "default"));

        var review = service.GetReview();

        Assert.Equal("Blocked", review.Status);
        Assert.Contains(review.Areas, area => area.Area == "Configuration" && area.Status == "Fail");
    }
}
