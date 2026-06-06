namespace AiKnowledgeTransfer.Application.Projects;

public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    string Source,
    Stream Content);
