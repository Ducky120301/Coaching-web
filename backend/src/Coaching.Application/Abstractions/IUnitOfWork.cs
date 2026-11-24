namespace Coaching.Application.Abstractions;

/// <summary>Lưu mọi thay đổi của một use case trong một transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
