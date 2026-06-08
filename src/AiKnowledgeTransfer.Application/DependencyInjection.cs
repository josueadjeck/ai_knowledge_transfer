namespace AiKnowledgeTransfer.Application;

using AiKnowledgeTransfer.Application.Audit;
using AiKnowledgeTransfer.Application.Compliance;
using AiKnowledgeTransfer.Application.Diagnostics;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Application.Documents;
using AiKnowledgeTransfer.Application.Exports;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Operations;
using AiKnowledgeTransfer.Application.Roadmaps;
using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Application.Traceability;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditLogService>();
        services.AddScoped<ComplianceMatrixService>();
        services.AddScoped<OperationalHealthService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<DocumentAnalysisService>();
        services.AddScoped<DocumentParserCapabilityService>();
        services.AddScoped<MarkdownExportService>();
        services.AddScoped<ExportApprovalService>();
        services.AddScoped<ExportHistoryService>();
        services.AddScoped<KnowledgeExtractionService>();
        services.AddScoped<KnowledgeReviewService>();
        services.AddScoped<KnowledgeReviewSummaryService>();
        services.AddScoped<PersistenceBackupService>();
        services.AddScoped<RoadmapService>();
        services.AddScoped<OnboardingReadinessService>();
        services.AddScoped<OnboardingStartPackageService>();
        services.AddScoped<RolePermissionService>();
        services.AddScoped<TraceabilityService>();

        return services;
    }
}
