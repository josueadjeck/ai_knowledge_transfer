namespace AiKnowledgeTransfer.Contracts.Projects;

public sealed record UploadDocumentResponse(
    DocumentResponse Document,
    string Message);
