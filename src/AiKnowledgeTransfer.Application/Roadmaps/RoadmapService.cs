namespace AiKnowledgeTransfer.Application.Roadmaps;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Roadmaps;
using AiKnowledgeTransfer.Domain.Roadmaps;

public sealed class RoadmapService(IProjectRepository projects)
{
    public async Task<RoadmapResponse?> GenerateAsync(Guid projectId, GenerateRoadmapRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var duration = Math.Clamp(request.DurationInWeeks, 1, 8);
        var weeks = Enumerable.Range(1, duration)
            .Select(week => CreateWeek(week, request.TargetRole, project.Documents.Count))
            .ToArray();

        var roadmap = new OnboardingRoadmap(request.TargetRole, duration, weeks);
        project.AddRoadmap(roadmap);

        await projects.SaveChangesAsync(cancellationToken);
        return RoadmapMapper.ToResponse(roadmap);
    }

    private static RoadmapWeek CreateWeek(int weekNumber, string targetRole, int documentCount)
    {
        return weekNumber switch
        {
            1 => new RoadmapWeek(
                weekNumber,
                "Systemueberblick und Quellenlage",
                [
                    $"Rolle {targetRole} im Projektkontext verstehen.",
                    $"Verfuegbare Quellen sichten ({documentCount} Dokumente registriert).",
                    "Wichtige Begriffe und offene Fragen erfassen."
                ],
                [
                    "Projektsteckbrief erstellen.",
                    "Top-10-Begriffe fuer das Glossar markieren."
                ],
                [
                    "Systemzweck kann erklaert werden.",
                    "Offene Fragen sind dokumentiert."
                ]),
            2 => new RoadmapWeek(
                weekNumber,
                "Architektur, Komponenten und Schnittstellen",
                [
                    "Zentrale Komponenten und Verantwortlichkeiten verstehen.",
                    "Kommunikationswege und Datenfluesse nachvollziehen."
                ],
                [
                    "Komponentenliste pruefen.",
                    "Ein einfaches Architekturdiagramm aus den Quellen ableiten."
                ],
                [
                    "Komponentenmodell ist reviewfaehig.",
                    "Unklare Schnittstellen sind markiert."
                ]),
            3 => new RoadmapWeek(
                weekNumber,
                "Workflows, Betrieb und Troubleshooting",
                [
                    "Wichtige Betriebsablaeufe Schritt fuer Schritt nachvollziehen.",
                    "Risiken und Kontrollpunkte je Workflow erkennen."
                ],
                [
                    "Einen Workflow als Runbook beschreiben.",
                    "Troubleshooting-Fragen aus offenen Punkten ableiten."
                ],
                [
                    "Mindestens ein Workflow ist als Checkliste formuliert.",
                    "Risiken sind mit Quellen verknuepft."
                ]),
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
                ])
        };
    }
}
