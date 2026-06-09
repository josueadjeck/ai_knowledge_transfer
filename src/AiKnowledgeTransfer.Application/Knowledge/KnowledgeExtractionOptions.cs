namespace AiKnowledgeTransfer.Application.Knowledge;

public sealed record KnowledgeExtractionOptions(
    int MaxChunksPerExtraction,
    int MaxTotalChunkCharacters)
{
    public const string MaxChunksEnvironmentVariable = "AKT_EXTRACTION_MAX_CHUNKS";
    public const string MaxTotalCharactersEnvironmentVariable = "AKT_EXTRACTION_MAX_CHARACTERS";

    public static KnowledgeExtractionOptions Default { get; } = new(80, 120_000);

    public static KnowledgeExtractionOptions FromEnvironment()
    {
        return new KnowledgeExtractionOptions(
            ReadPositiveInt(MaxChunksEnvironmentVariable, Default.MaxChunksPerExtraction),
            ReadPositiveInt(MaxTotalCharactersEnvironmentVariable, Default.MaxTotalChunkCharacters));
    }

    private static int ReadPositiveInt(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }
}
