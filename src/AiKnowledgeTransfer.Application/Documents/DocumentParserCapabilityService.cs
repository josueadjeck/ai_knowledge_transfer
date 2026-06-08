namespace AiKnowledgeTransfer.Application.Documents;

using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Contracts.Projects;

public sealed class DocumentParserCapabilityService(IEnumerable<IDocumentParser> parsers)
{
    public IReadOnlyCollection<DocumentParserCapabilityResponse> List()
    {
        return parsers
            .OrderBy(parser => parser.Name, StringComparer.Ordinal)
            .Select(parser => new DocumentParserCapabilityResponse(
                parser.Name,
                parser.SupportedContentTypes,
                parser.SupportedFileExtensions,
                parser.CapabilityDescription))
            .ToArray();
    }
}
