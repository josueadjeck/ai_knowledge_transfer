namespace AiKnowledgeTransfer.Domain.Projects;

using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;
using AiKnowledgeTransfer.Domain.Roadmaps;

public sealed class KnowledgeProject
{
    private readonly List<DocumentVersion> _documents = [];
    private readonly List<KnowledgeItem> _knowledgeItems = [];
    private readonly List<OnboardingRoadmap> _roadmaps = [];

    public KnowledgeProject(string name, string description, string owner)
    {
        Id = Guid.NewGuid();
        Name = RequireText(name, nameof(name));
        Description = description.Trim();
        Owner = RequireText(owner, nameof(owner));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private KnowledgeProject(
        Guid id,
        string name,
        string description,
        string owner,
        DateTimeOffset createdAt,
        IEnumerable<DocumentVersion> documents,
        IEnumerable<KnowledgeItem> knowledgeItems,
        IEnumerable<OnboardingRoadmap> roadmaps)
    {
        Id = id;
        Name = name;
        Description = description;
        Owner = owner;
        CreatedAt = createdAt;
        _documents.AddRange(documents);
        _knowledgeItems.AddRange(knowledgeItems);
        _roadmaps.AddRange(roadmaps);
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public string Owner { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<DocumentVersion> Documents => _documents;

    public IReadOnlyCollection<KnowledgeItem> KnowledgeItems => _knowledgeItems;

    public IReadOnlyCollection<OnboardingRoadmap> Roadmaps => _roadmaps;

    public DocumentVersion RegisterDocument(string fileName, string contentType, string source, long sizeInBytes, string storagePath)
    {
        var versionNumber = _documents.Count(document => document.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) + 1;
        var document = new DocumentVersion(fileName, contentType, source, sizeInBytes, versionNumber, storagePath);

        _documents.Add(document);
        return document;
    }

    public KnowledgeItem AddKnowledgeItem(KnowledgeItemType type, string title, string summary, Guid? sourceDocumentId)
    {
        var item = new KnowledgeItem(type, title, summary, sourceDocumentId);
        _knowledgeItems.Add(item);
        return item;
    }

    public void AddRoadmap(OnboardingRoadmap roadmap)
    {
        _roadmaps.Add(roadmap);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }

    public static KnowledgeProject Rehydrate(
        Guid id,
        string name,
        string description,
        string owner,
        DateTimeOffset createdAt,
        IEnumerable<DocumentVersion> documents,
        IEnumerable<KnowledgeItem> knowledgeItems,
        IEnumerable<OnboardingRoadmap> roadmaps)
    {
        return new KnowledgeProject(
            id,
            name,
            description,
            owner,
            createdAt,
            documents,
            knowledgeItems,
            roadmaps);
    }
}
