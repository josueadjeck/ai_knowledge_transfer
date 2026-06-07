namespace AiKnowledgeTransfer.Contracts.Errors;

public sealed record ErrorResponse(
    string Code,
    string Message);
