namespace AiKnowledgeTransfer.Application.Roadmaps;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Projects;
using AiKnowledgeTransfer.Domain.Roadmaps;

public sealed class RoadmapService(
    IProjectRepository projects,
    IAuditLog? auditLog = null)
{
    private readonly IAuditLog _auditLog = auditLog ?? NullAuditLog.Instance;

    public async Task<RoadmapResponse?> GenerateAsync(Guid projectId, GenerateRoadmapRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var duration = Math.Clamp(request.DurationInWeeks, 1, 8);
        var context = RoadmapKnowledgeContext.FromProject(project);
        var weeks = Enumerable.Range(1, duration)
            .Select(week => CreateWeek(week, request.TargetRole, project.Documents.Count, context))
            .ToArray();

        var roadmap = new OnboardingRoadmap(request.TargetRole, duration, weeks);
        project.AddRoadmap(roadmap);

        await projects.SaveChangesAsync(cancellationToken);
        await _auditLog.AppendAsync(
            AuditEvent.Create(project.Id, "RoadmapGenerated", "system", "Roadmap", roadmap.Id, $"Roadmap for '{roadmap.TargetRole}' was generated."),
            cancellationToken);
        return RoadmapMapper.ToResponse(roadmap);
    }

    private static RoadmapWeek CreateWeek(int weekNumber, string targetRole, int documentCount, RoadmapKnowledgeContext context)
    {
        return weekNumber switch
        {
            1 => new RoadmapWeek(
                weekNumber,
                "Systemueberblick und Quellenlage",
                AddApprovedItems(
                [
                    $"Rolle {targetRole} im Projektkontext verstehen.",
                    $"Verfuegbare Quellen sichten ({documentCount} Dokumente registriert).",
                    "Wichtige Begriffe und offene Fragen erfassen."
                ], context.GlossaryTerms, "Freigegebenen Begriff verstehen"),
                [
                    "Projektsteckbrief erstellen.",
                    "Top-10-Begriffe fuer das Glossar markieren."
                ],
                [
                    "Systemzweck kann erklaert werden.",
                    "Offene Fragen sind dokumentiert."
                ],
                context.ReviewNotes),
            2 => new RoadmapWeek(
                weekNumber,
                "Architektur, Komponenten und Schnittstellen",
                AddApprovedItems(
                [
                    "Zentrale Komponenten und Verantwortlichkeiten verstehen.",
                    "Kommunikationswege und Datenfluesse nachvollziehen."
                ], context.Components, "Freigegebene Komponente einordnen"),
                AddApprovedItems(
                [
                    "Komponentenliste pruefen.",
                    "Ein einfaches Architekturdiagramm aus den Quellen ableiten."
                ], context.Components, "Komponente im Diagramm verorten"),
                [
                    "Komponentenmodell ist reviewfaehig.",
                    "Unklare Schnittstellen sind markiert."
                ],
                context.ReviewNotes),
            3 => new RoadmapWeek(
                weekNumber,
                "Workflows, Betrieb und Troubleshooting",
                AddApprovedItems(
                [
                    "Wichtige Betriebsablaeufe Schritt fuer Schritt nachvollziehen.",
                    "Risiken und Kontrollpunkte je Workflow erkennen."
                ], context.Workflows, "Freigegebenen Workflow nachvollziehen"),
                AddApprovedItems(
                [
                    "Einen Workflow als Runbook beschreiben.",
                    "Troubleshooting-Fragen aus offenen Punkten ableiten."
                ], context.Workflows, "Workflow als Checkliste ueben"),
                [
                    "Mindestens ein Workflow ist als Checkliste formuliert.",
                    "Risiken sind mit Quellen verknuepft."
                ],
                context.ReviewNotes),
            _ => new RoadmapWeek(
                weekNumber,
                "Review, Uebung und Freigabe",
                [
                    "Gelerntes praktisch anwenden.",
                    "AI-generierte Inhalte kritisch pruefen.",
                    "Freigabekriterien verstehen."
                ],
                [
                    "Roadmap mit einem Senior Engineer reviewen.",
                    "Pruefungsfragen beantworten und offene Luecken priorisieren."
                ],
                [
                    "Review-Kommentare sind eingearbeitet.",
                    "Abschlusskriterien sind erfuellt oder begruendet offen."
                ],
                context.ReviewNotes)
        };
    }

    private static IReadOnlyCollection<string> AddApprovedItems(
        IReadOnlyCollection<string> baseItems,
        IReadOnlyCollection<string> approvedItems,
        string prefix)
    {
        return baseItems
            .Concat(approvedItems.Select(item => $"{prefix}: {item}."))
            .ToArray();
    }

    private sealed record RoadmapKnowledgeContext(
        IReadOnlyCollection<string> GlossaryTerms,
        IReadOnlyCollection<string> Components,
        IReadOnlyCollection<string> Workflows,
        IReadOnlyCollection<string> ReviewNotes)
    {
        public static RoadmapKnowledgeContext FromProject(KnowledgeProject project)
        {
            var finalItems = project.KnowledgeItems
                .Where(KnowledgeQualityPolicy.IsFinal)
                .ToArray();

            var reviewNotes = project.KnowledgeItems
                .Where(item => !KnowledgeQualityPolicy.IsFinal(item))
                .Select(KnowledgeQualityPolicy.ToReviewRisk)
                .Take(10)
                .ToArray();

            return new RoadmapKnowledgeContext(
                finalItems
                    .Where(item => item.Type == KnowledgeItemType.GlossaryTerm)
                    .Select(item => item.Title)
                    .Take(5)
                    .ToArray(),
                finalItems
                    .Where(item => item.Type == KnowledgeItemType.Component)
                    .Select(item => item.Title)
                    .Take(5)
                    .ToArray(),
                finalItems
                    .Where(item => item.Type == KnowledgeItemType.Workflow)
                    .Select(item => item.Title)
                    .Take(5)
                    .ToArray(),
                reviewNotes);
        }
    }
}
