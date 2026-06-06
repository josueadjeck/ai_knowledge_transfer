namespace AiKnowledgeTransfer.Application.Projects;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;

public sealed class ProjectService(IProjectRepository projects)
{
    public async Task<IReadOnlyCollection<ProjectSummaryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await projects.ListAsync(cancellationToken);
        return result.Select(ProjectMapper.ToSummary).ToArray();
    }

    public async Task<ProjectSummaryResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = new KnowledgeProject(request.Name, request.Description, request.Owner);
        SeedStarterKnowledge(project);

        await projects.AddAsync(project, cancellationToken);
        await projects.SaveChangesAsync(cancellationToken);

        return ProjectMapper.ToSummary(project);
    }

    public async Task<DocumentResponse?> RegisterDocumentAsync(Guid projectId, RegisterDocumentRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var document = project.RegisterDocument(
            request.FileName,
            request.ContentType,
            request.Source,
            request.SizeInBytes);

        await projects.SaveChangesAsync(cancellationToken);
        return ProjectMapper.ToResponse(document);
    }

    private static void SeedStarterKnowledge(KnowledgeProject project)
    {
        project.AddKnowledgeItem(
            KnowledgeItemType.Workflow,
            "Dokumentenaufnahme",
            "Quellen werden versioniert gespeichert und spaeter fuer AI-gestuetzte Analyse genutzt.",
            sourceDocumentId: null);

        project.AddKnowledgeItem(
            KnowledgeItemType.OpenQuestion,
            "Compliance Scope",
            "Zu klaeren: Welche Normen, Audit-Anforderungen und Freigabeprozesse gelten fuer dieses Projekt?",
            sourceDocumentId: null);
    }
}
