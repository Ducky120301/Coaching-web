using Coaching.Domain.Coaches;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class CoachRepository(AppDbContext db) : ICoachRepository
{
    public Task<Coach?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Coaches.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public void Add(Coach coach) => db.Coaches.Add(coach);
}
