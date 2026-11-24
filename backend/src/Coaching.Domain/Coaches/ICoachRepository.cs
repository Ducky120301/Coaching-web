namespace Coaching.Domain.Coaches;

public interface ICoachRepository
{
    Task<Coach?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(Coach coach);
}
