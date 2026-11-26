using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Seeding;

/// <summary>
/// @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: danh mục ban đầu (11 nhóm cơ, 7 dụng cụ) cho mọi coach chưa có
/// mục nào — chạy lúc khởi động ở MỌI môi trường (quy tắc nghiệp vụ, khác seed bài tập dev). Idempotent.
/// </summary>
public static class ExerciseCatalogSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var coachIds = await db.Coaches
            .Where(coach => !db.ExerciseCategories.Any(c => c.CoachId == coach.Id))
            .Select(coach => coach.Id)
            .ToListAsync(ct);
        if (coachIds.Count == 0)
            return;

        foreach (var coachId in coachIds)
        {
            db.ExerciseCategories.AddRange(DefaultCatalog.MuscleGroups
                .Select((name, i) => ExerciseCategory.Create(coachId, CategoryKind.MuscleGroup, name, i + 1)));
            db.ExerciseCategories.AddRange(DefaultCatalog.Equipment
                .Select((name, i) => ExerciseCategory.Create(coachId, CategoryKind.Equipment, name, i + 1)));
        }
        await db.SaveChangesAsync(ct);
    }
}
