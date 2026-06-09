namespace AiKnowledgeTransfer.Application.Operations;

using AiKnowledgeTransfer.Contracts.Operations;

public sealed class RoadmapReadinessService
{
    public RoadmapReadinessResponse GetStatus()
    {
        var phases = BuildPhases();
        var needsWorkCount = phases.Count(phase => phase.Status == "NeedsWork");
        var readyCount = phases.Count - needsWorkCount;
        var status = needsWorkCount == 0 ? "Complete" : "NeedsWork";

        return new RoadmapReadinessResponse(
            status,
            DateTimeOffset.UtcNow,
            phases.Count,
            readyCount,
            needsWorkCount,
            phases,
            BuildSummary(status, readyCount, needsWorkCount).ToArray());
    }

    private static IReadOnlyCollection<RoadmapPhaseReadinessResponse> BuildPhases()
    {
        return
        [
            Ready(
                0,
                "Produktklaerung",
                4,
                5,
                [
                    "Product vision, target groups, MVP scope and non-MVP scope are documented in ROADMAP.md.",
                    "Core product decision is documented: AI proposes, humans approve."
                ],
                [
                    "Standalone stakeholder map, risk list and requirement catalog are still lightweight inside ROADMAP.md rather than separate artifacts."
                ]),
            Ready(
                1,
                "Architektur und Projektfundament",
                7,
                7,
                [
                    "Solution structure, layered projects, DI, architecture tests and ADRs are present.",
                    "AI provider, persistence, auth and operations abstractions are in Application/Infrastructure."
                ],
                []),
            Ready(
                2,
                "UX-Prototyp",
                6,
                6,
                [
                    "Blazor MVP workflow covers project, upload, analysis, extraction, review, roadmap, traceability, export and operations.",
                    "Role-based UI action availability is wired to the authorization service."
                ],
                []),
            Ready(
                3,
                "Dokumentenverwaltung",
                5,
                6,
                [
                    "Projects, uploads, document metadata, versions, validation, local storage and audit events are implemented.",
                    "Document cards show metadata and storage state."
                ],
                [
                    "Browsing or restoring old document version content is not yet a full user-facing workflow."
                ]),
            Ready(
                4,
                "Parsing und Knowledge Extraction",
                6,
                6,
                [
                    "TXT, Markdown, PDF and Word parsing produce chunks with source references and quality labels.",
                    "Provider-independent extraction uses OpenAI when configured and heuristic fallback otherwise."
                ],
                []),
            Ready(
                5,
                "Roadmap Generator",
                6,
                6,
                [
                    "Role and duration based roadmap generation exists and favors approved verified knowledge.",
                    "Onboarding readiness and start package are available for pilot onboarding.",
                    "Editable roadmap structure is implemented through API and Blazor UI for generated roadmap weeks."
                ],
                []),
            Ready(
                6,
                "Review und Freigabe",
                5,
                5,
                [
                    "Knowledge items support review, approval, rejection, comments, quality status and review history.",
                    "Bulk review actions are available in API and UI.",
                    "Document version comparison is available as a dedicated API and Blazor workflow."
                ],
                []),
            Ready(
                7,
                "Export",
                4,
                4,
                [
                    "Markdown export, Word export, export history, export approval and traceability details are implemented.",
                    "Word export creates a DOCX handover document with sources, verified knowledge, review notes, review history, traceability and roadmap sections."
                ],
                []),
            Ready(
                8,
                "Compliance und Traceability",
                5,
                5,
                [
                    "Traceability matrix, compliance matrix, audit view, security concept and data protection concept are implemented."
                ],
                []),
            NeedsWork(
                9,
                "Betrieb, Sicherheit und Skalierung",
                8,
                9,
                [
                    "Monitoring, backups, release readiness, DB providers, deployment strategy gates, security gates, dependency SBOM, build artifact file hashes, container checks, container provenance, secret-store release metadata, role review and tenant isolation review are implemented.",
                    "SQL Server and PostgreSQL are configurable production-capable database providers."
                ],
                [
                    "Enterprise-hardening follow-ups remain: registry-backed image signing enforcement, standards-complete CycloneDX/SPDX SBOM attestation, automated provider-specific secret retrieval and complete tenant isolation."
                ])
        ];
    }

    private static RoadmapPhaseReadinessResponse Ready(
        int phase,
        string name,
        int completedCriteria,
        int totalCriteria,
        IReadOnlyCollection<string> evidence,
        IReadOnlyCollection<string> gaps)
    {
        return new RoadmapPhaseReadinessResponse(
            phase,
            name,
            gaps.Count == 0 ? "Ready" : "ReadyWithNotes",
            completedCriteria,
            totalCriteria,
            evidence,
            gaps);
    }

    private static RoadmapPhaseReadinessResponse NeedsWork(
        int phase,
        string name,
        int completedCriteria,
        int totalCriteria,
        IReadOnlyCollection<string> evidence,
        IReadOnlyCollection<string> gaps)
    {
        return new RoadmapPhaseReadinessResponse(
            phase,
            name,
            "NeedsWork",
            completedCriteria,
            totalCriteria,
            evidence,
            gaps);
    }

    private static IEnumerable<string> BuildSummary(string status, int readyCount, int needsWorkCount)
    {
        yield return $"Roadmap status is {status}: {readyCount} phases are ready or ready with notes; {needsWorkCount} phases still need work.";
        yield return "Use this audit as the operational handoff list before claiming the full roadmap is complete.";
    }
}
