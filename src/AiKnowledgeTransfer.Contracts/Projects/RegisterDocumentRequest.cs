namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record RegisterDocumentRequest(
    string FileName,
    string ContentType,
    string Source,
    long SizeInBytes);
