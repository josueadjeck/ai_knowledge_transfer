namespace AiKnowledgeTransfer.Domain.Documents;

public sealed class DocumentVersion
{
    private readonly List<DocumentChunk> _chunks = [];

    private DocumentVersion(
        Guid id,
        string fileName,
        string contentType,
        string source,
        long sizeInBytes,
        int versionNumber,
        string storagePath,
        DateTimeOffset uploadedAt,
        DocumentStatus status,
        IEnumerable<DocumentChunk> chunks)
    {
        Id = id;
        FileName = fileName;
        ContentType = contentType;
        Source = source;
        SizeInBytes = sizeInBytes;
        VersionNumber = versionNumber;
        StoragePath = storagePath;
        UploadedAt = uploadedAt;
        Status = status;
        _chunks.AddRange(chunks);
    }

    public DocumentVersion(string fileName, string contentType, string source, long sizeInBytes, int versionNumber, string storagePath)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must not be empty.", nameof(fileName));
        }

        if (sizeInBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "File size must not be negative.");
        }

        Id = Guid.NewGuid();
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        Source = source.Trim();
        SizeInBytes = sizeInBytes;
        VersionNumber = versionNumber;
        StoragePath = storagePath.Trim();
        UploadedAt = DateTimeOffset.UtcNow;
        Status = DocumentStatus.Registered;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public string Source { get; }

    public long SizeInBytes { get; }

    public int VersionNumber { get; }

    public string StoragePath { get; }

    public DateTimeOffset UploadedAt { get; }

    public DocumentStatus Status { get; private set; }

    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks;

    public void ReplaceChunks(IEnumerable<DocumentChunk> chunks)
    {
        _chunks.Clear();
        _chunks.AddRange(chunks);
        MarkAnalyzed();
    }

    public void MarkAnalyzed()
    {
        Status = DocumentStatus.Analyzed;
    }

    public static DocumentVersion Rehydrate(
        Guid id,
        string fileName,
        string contentType,
        string source,
        long sizeInBytes,
        int versionNumber,
        string storagePath,
        DateTimeOffset uploadedAt,
        DocumentStatus status,
        IEnumerable<DocumentChunk> chunks)
    {
        return new DocumentVersion(
            id,
            fileName,
            contentType,
            source,
            sizeInBytes,
            versionNumber,
            storagePath,
            uploadedAt,
            status,
            chunks);
    }
}
