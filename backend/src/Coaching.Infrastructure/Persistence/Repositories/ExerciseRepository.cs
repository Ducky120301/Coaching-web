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

    // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019 @ADR-f03-thu-vien-bai-tap-002:
    // 1 truy vấn theo index (CoachId, IsHidden) + LIKE '%q%' trên SearchKey (EF escape ký tự đặc biệt của LIKE) — NFR-f03-thu-vien-bai-tap-001
    public async Task<IReadOnlyList<ExerciseListRow>> ListAsync(Guid coachId, ExerciseFilter filter, CancellationToken ct = default)
    {
        var query = db.Exercises.AsNoTracking().Where(e => e.CoachId == coachId && e.IsHidden == filter.Hidden);
        if (filter.NormalizedQuery.Length > 0)
            query = query.Where(e => e.SearchKey.Contains(filter.NormalizedQuery));
        if (filter.MuscleGroupId is { } muscleGroupId)
            query = query.Where(e => e.MuscleGroupId == muscleGroupId);
        if (filter.EquipmentId is { } equipmentId)
            query = query.Where(e => e.EquipmentId == equipmentId);

        return await query
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Id)
            .Select(e => new ExerciseListRow(e.Id, e.Name, e.MuscleGroupId, e.EquipmentId, e.VideoUrl != null, e.IsHidden))
            .ToListAsync(ct);
    }

    public void Add(Exercise e) => db.Exercises.Add(e);

    public void Remove(Exercise e) => db.Exercises.Remove(e);
}
