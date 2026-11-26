using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseRepository(AppDbContext db) : IExerciseRepository
{
    public Task<Exercise?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default) =>
        db.Exercises.SingleOrDefaultAsync(e => e.Id == id && e.CoachId == coachId, ct);

    public async Task<Exercise?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken ct = default)
    {
        var exercise = await GetAsync(id, coachId, ct);
        if (exercise is not null)
            db.Entry(exercise).Property(e => e.Version).OriginalValue = expectedVersion; // @BR-f03-thu-vien-bai-tap-020
        return exercise;
    }

    // @BR-f03-thu-vien-bai-tap-001: theo chỉ mục duy nhất (CoachId, NameKey, EquipmentId) — gồm cả bài đang ẩn
    public Task<Exercise?> FindByKeyAsync(Guid coachId, string nameKey, Guid equipmentId, CancellationToken ct = default) =>
        db.Exercises.AsNoTracking()
            .SingleOrDefaultAsync(e => e.CoachId == coachId && e.NameKey == nameKey && e.EquipmentId == equipmentId, ct);

    public void Add(Exercise e) => db.Exercises.Add(e);

    public void Remove(Exercise e) => db.Exercises.Remove(e);
}
