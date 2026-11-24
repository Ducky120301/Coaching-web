namespace Coaching.Domain.Common;

/// <summary>
/// Gốc của aggregate. Mọi thay đổi trạng thái đi qua method nghiệp vụ của aggregate;
/// aggregate khác chỉ được tham chiếu bằng Id.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
