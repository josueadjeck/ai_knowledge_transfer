namespace AiKnowledgeTransfer.Application.Documents;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Application.Knowledge;
using AiKnowledgeTransfer.Application.Projects;
using AiKnowledgeTransfer.Contracts.Projects;

public sealed class DocumentAnalysisPreflightService(
    IProjectRepository projects,
    IEnumerable<IDocumentParser> parsers,
    DocumentAnalysisOptions? analysisOptions = null,
    KnowledgeExtractionOptions? extractionOptions = null)
{
    private readonly DocumentAnalysisOptions _analysisOptions = analysisOptions ?? DocumentAnalysisOptions.Default;
    private readonly KnowledgeExtractionOptions _extractionOptions = extractionOptions ?? KnowledgeExtractionOptions.Default;

    public async Task<DocumentAnalysisPreflightResponse?> GetAsync(
        Guid projectId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var document = project?.Documents.FirstOrDefault(candidate => candidate.Id == documentId);
        if (project is null || document is null)
        {
            return null;
        }

        var parser = parsers.FirstOrDefault(candidate => candidate.CanParse(document.ContentType, document.FileName));
        var analysisUtilization = UtilizationPercent(document.SizeInBytes, _analysisOptions.MaxTotalExtractedCharacters);
        var extractionUtilization = UtilizationPercent(document.SizeInBytes, _extractionOptions.MaxTotalChunkCharacters);
        var recommendations = BuildRecommendations(parser is not null, analysisUtilization, extractionUtilization).ToArray();
        var status = DetermineStatus(parser is not null, analysisUtilization, extractionUtilization);

        return new DocumentAnalysisPreflightResponse(
            document.Id,
            document.FileName,
            status,
            DetermineWorkloadClass(analysisUtilization, extractionUtilization),
            document.SizeInBytes,
            _analysisOptions.MaxTotalExtractedCharacters,
            _extractionOptions.MaxTotalChunkCharacters,
            analysisUtilization,
            extractionUtilization,
            ParserAvailable: parser is not null,
            parser?.Name ?? "None",
            recommendations);
    }

    private static decimal UtilizationPercent(long sizeInBytes, int limit)
    {
        if (limit <= 0)
        {
            return 100;
        }

        return Math.Round(sizeInBytes * 100m / limit, 1, MidpointRounding.AwayFromZero);
    }

    private static string DetermineStatus(bool parserAvailable, decimal analysisUtilization, decimal extractionUtilization)
    {
        if (!parserAvailable || analysisUtilization > 100)
        {
            return "Blocked";
        }

        return extractionUtilization >= 80 || analysisUtilization >= 80
            ? "ReviewRecommended"
            : "Ready";
    }

    private static string DetermineWorkloadClass(decimal analysisUtilization, decimal extractionUtilization)
    {
        var highestUtilization = Math.Max(analysisUtilization, extractionUtilization);
        return highestUtilization switch
        {
            >= 100 => "VeryLarge",
            >= 80 => "Large",
            >= 50 => "Medium",
            _ => "Small"
        };
    }

    private static IEnumerable<string> BuildRecommendations(
        bool parserAvailable,
        decimal analysisUtilization,
        decimal extractionUtilization)
    {
        if (!parserAvailable)
        {
            yield return "No parser is registered for this document type.";
            yield break;
        }

        if (analysisUtilization > 100)
        {
            yield return "Split or shorten the document before analysis, or deliberately raise the analysis character limit.";
            yield break;
        }

        if (analysisUtilization >= 80)
        {
            yield return "Document is close to the analysis character limit; consider splitting it into smaller source documents.";
        }

        if (extractionUtilization >= 80)
        {
            yield return "Document may be expensive for provider extraction; review chunk quality before calling AI providers.";
        }

        if (analysisUtilization < 80 && extractionUtilization < 80)
        {
            yield return "Document is within configured analysis and extraction guardrails.";
        }
    }
}
