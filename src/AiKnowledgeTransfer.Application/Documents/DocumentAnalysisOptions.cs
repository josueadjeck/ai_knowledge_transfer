namespace AiKnowledgeTransfer.Application.Documents;

public sealed record DocumentAnalysisOptions(
    int MaxChunksPerDocument,
    int MaxTotalExtractedCharacters)
{
    public const string MaxChunksEnvironmentVariable = "AKT_ANALYSIS_MAX_CHUNKS";
    public const string MaxTotalCharactersEnvironmentVariable = "AKT_ANALYSIS_MAX_CHARACTERS";

    public static DocumentAnalysisOptions Default { get; } = new(250, 500_000);

    public static DocumentAnalysisOptions FromEnvironment()
    {
        return new DocumentAnalysisOptions(
            ReadPositiveInt(MaxChunksEnvironmentVariable, Default.MaxChunksPerDocument),
            ReadPositiveInt(MaxTotalCharactersEnvironmentVariable, Default.MaxTotalExtractedCharacters));
    }

    private static int ReadPositiveInt(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }
}
