using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Persistence;

namespace Coaching.Infrastructure.Audit;

internal sealed class AuditLog(AppDbContext db, ICurrentUser currentUser, IClock clock) : IAuditLog
{
    public void Record(string action, string entityType, string entityId, string? details = null)
    {
        db.AuditLogs.Add(new AuditLogEntry
        {
            OccurredAt = clock.UtcNow,
            ActorUserId = currentUser.UserId,
            ActorEmail = currentUser.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
        });
    }
}
