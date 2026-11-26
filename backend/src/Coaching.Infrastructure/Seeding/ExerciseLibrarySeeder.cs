using System.Text.Json;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Seeding;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-007: thư viện bài tập ban đầu của coach — 99 bài prototype đã gán theo danh mục ban đầu (Phụ lục
/// tech-design). Chạy lúc khởi động ở MỌI môi trường; mỗi coach được xét đúng một lần (bảng ExerciseLibrarySeeds): thư viện trống
/// → nạp 99 bài; đã có bài → chỉ đánh dấu. Không bao giờ nạp lại, ghi đè hay nhân đôi. Host test tắt bằng Seed:ExerciseLibrary=false.
/// Chạy SAU ExerciseCatalogSeeder (cần danh mục ban đầu).
/// </summary>
public static class ExerciseLibrarySeeder
{
    public const string EnabledKey = "Seed:ExerciseLibrary";
    internal const string ResourceName = "Coaching.Infrastructure.Seeding.exercises.seed.json";

    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct = default)
    {
        var coachIds = await db.Coaches
            .Where(coach => !db.Set<ExerciseLibrarySeed>().Any(s => s.CoachId == coach.Id))
            .Select(coach => coach.Id)
            .ToListAsync(ct);
        if (coachIds.Count == 0)
            return;

        var seeds = Load();
        foreach (var coachId in coachIds)
            await SeedCoachAsync(db, coachId, seeds, now, ct);
    }

    internal static async Task<int> SeedCoachAsync(AppDbContext db, Guid coachId, IReadOnlyList<SeedExercise> seeds,
        DateTimeOffset now, CancellationToken ct)
    {
        var added = 0;
        if (!await db.Exercises.AnyAsync(e => e.CoachId == coachId, ct))
        {
            var categories = await db.ExerciseCategories.AsNoTracking().Where(c => c.CoachId == coachId).ToListAsync(ct);
            var muscleGroups = categories.Where(c => c.Kind == CategoryKind.MuscleGroup).ToDictionary(c => c.Name, c => c.Id);
            var equipment = categories.Where(c => c.Kind == CategoryKind.Equipment).ToDictionary(c => c.Name, c => c.Id);
            foreach (var seed in seeds)
            {
                // cùng luật với coach nhập tay (BR-f03-thu-vien-bai-tap-002, 007, 009…012)
                var exercise = Exercise.Create(coachId, seed.Name, muscleGroups[seed.MuscleGroup], equipment[seed.Equipment]);
                exercise.SetVideo(seed.VideoUrl);
                exercise.SetDose(ExerciseDose.Create(seed.Sets, seed.RepsMin, seed.RepsMax, seed.RirMin, seed.RirMax, seed.RestSeconds));
                db.Exercises.Add(exercise);
            }
            added = seeds.Count;
        }

        db.Set<ExerciseLibrarySeed>().Add(new ExerciseLibrarySeed(coachId, now, added));
        await db.SaveChangesAsync(ct);
        return added;
    }

    internal static IReadOnlyList<SeedExercise> Load()
    {
        using var stream = typeof(ExerciseLibrarySeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Thiếu resource nhúng {ResourceName}.");
        return JsonSerializer.Deserialize<List<SeedExercise>>(stream, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"{ResourceName} rỗng.");
    }

    /// <summary>Một dòng của exercises.seed.json: liều "3|10–12|1|90s" đã tách thành số; "—" là null.</summary>
    internal sealed record SeedExercise(
        string Name, string MuscleGroup, string Equipment, string? VideoUrl,
        int? Sets, int? RepsMin, int? RepsMax, int? RirMin, int? RirMax, int? RestSeconds);
}
