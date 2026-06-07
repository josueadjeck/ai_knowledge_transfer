namespace AiKnowledgeTransfer.Infrastructure.Persistence.Database;

public sealed class AuditEventRecord
{
    public Guid Id { get; set; }

    public Guid? ProjectId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Actor { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public Guid? TargetId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}
