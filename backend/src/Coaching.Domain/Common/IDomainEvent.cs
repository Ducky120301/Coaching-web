namespace Coaching.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
