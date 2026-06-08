namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record DocumentParserCapabilityResponse(
    string ParserName,
    IReadOnlyCollection<string> ContentTypes,
    IReadOnlyCollection<string> FileExtensions,
    string Detail);
