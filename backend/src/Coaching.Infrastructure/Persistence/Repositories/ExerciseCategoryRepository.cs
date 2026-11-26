using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseCategoryRepository(AppDbContext db) : IExerciseCategoryRepository
{
    public async Task<IReadOnlyList<ExerciseCategory>> ListAsync(Guid coachId, CancellationToken ct = default) =>
        await db.ExerciseCategories.AsNoTracking()
            .Where(c => c.CoachId == coachId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public Task<ExerciseCategory?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default) =>
        db.ExerciseCategories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.CoachId == coachId, ct);

    public Task<bool> ExistsAsync(Guid coachId, CategoryKind kind, string nameKey, CancellationToken ct = default) =>
        db.ExerciseCategories.AnyAsync(c => c.CoachId == coachId && c.Kind == kind && c.NameKey == nameKey, ct);

    public void Add(ExerciseCategory c) => db.ExerciseCategories.Add(c);
}
