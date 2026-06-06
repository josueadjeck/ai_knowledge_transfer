namespace AiKnowledgeTransfer.Infrastructure.Knowledge;

public sealed record OpenAiKnowledgeExtractorOptions(
    string ApiKey,
    string Model,
    Uri BaseUrl);
