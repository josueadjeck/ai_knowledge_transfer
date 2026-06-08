namespace AiKnowledgeTransfer.Infrastructure.Knowledge;

using System.Text.RegularExpressions;
using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;

public sealed partial class HeuristicKnowledgeExtractor : IKnowledgeExtractor
{
    public Task<KnowledgeExtractionResult> ExtractAsync(
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        var items = new List<ExtractedKnowledgeItem>();
        var combinedText = string.Join(Environment.NewLine, chunks.Select(chunk => chunk.Text));

        AddTechnicalTerms(combinedText, items);
        AddWorkflowCandidates(chunks, items);
        AddOpenQuestions(chunks, items);

        var distinctItems = items
            .GroupBy(item => (item.Type, NormalizedTitle: item.Title.ToUpperInvariant()))
            .Select(group => group.First())
            .Take(20)
            .ToArray();

        return Task.FromResult(new KnowledgeExtractionResult(
            distinctItems,
            "Heuristic",
            Detail: "Local rule-based extractor."));
    }

    private static void AddTechnicalTerms(string text, List<ExtractedKnowledgeItem> items)
    {
        foreach (var match in TechnicalTermRegex().Matches(text).Cast<Match>())
        {
            var term = match.Value.Trim();
            var type = term.Length <= 5 ? KnowledgeItemType.GlossaryTerm : KnowledgeItemType.Component;

            items.Add(new ExtractedKnowledgeItem(
                type,
                term,
                $"Candidate extracted from document text. Needs expert review for project-specific meaning of '{term}'."));
        }
    }

    private static void AddWorkflowCandidates(IEnumerable<DocumentChunk> chunks, List<ExtractedKnowledgeItem> items)
    {
        foreach (var chunk in chunks.Where(chunk => ContainsAny(chunk.Text, "workflow", "deployment", "recovery", "onboarding")))
        {
            items.Add(new ExtractedKnowledgeItem(
                KnowledgeItemType.Workflow,
                $"Workflow candidate from chunk {chunk.ChunkNumber}",
                Summarize(chunk.Text)));
        }
    }

    private static void AddOpenQuestions(IEnumerable<DocumentChunk> chunks, List<ExtractedKnowledgeItem> items)
    {
        foreach (var chunk in chunks.Where(chunk => ContainsAny(chunk.Text, "must be reviewed", "to clarify", "unknown", "open question")))
        {
            items.Add(new ExtractedKnowledgeItem(
                KnowledgeItemType.OpenQuestion,
                $"Review needed for chunk {chunk.ChunkNumber}",
                $"Clarify or review this source statement: {Summarize(chunk.Text)}"));
        }
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static string Summarize(string text)
    {
        var normalized = WhitespaceRegex().Replace(text.Trim(), " ");
        return normalized.Length <= 180 ? normalized : $"{normalized[..177]}...";
    }

    [GeneratedRegex(@"\b[A-Z][A-Za-z0-9]{1,}(?:[A-Z0-9][A-Za-z0-9]*)\b")]
    private static partial Regex TechnicalTermRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
