namespace Coaching.Infrastructure.Audit;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorEmail { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? Details { get; set; }
}
