using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

/// <summary>@BR-f03-thu-vien-bai-tap-015: ẩn bài (không cần version — chỉ đổi cờ ẩn).</summary>
public sealed class HideExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, CancellationToken ct = default) =>
        ExerciseVisibility.ChangeAsync(currentCoach, exercises, categories, unitOfWork, id, e => e.Hide(), ct);
}

/// <summary>@BR-f03-thu-vien-bai-tap-015: bỏ ẩn bài.</summary>
public sealed class UnhideExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, CancellationToken ct = default) =>
        ExerciseVisibility.ChangeAsync(currentCoach, exercises, categories, unitOfWork, id, e => e.Unhide(), ct);
}

internal static class ExerciseVisibility
{
    public static async Task<Result<ExerciseDetailDto>> ChangeAsync(CurrentCoach currentCoach, IExerciseRepository exercises,
        IExerciseCategoryRepository categories, IUnitOfWork unitOfWork, Guid id, Action<Exercise> change, CancellationToken ct)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;
        // NFR-f03-thu-vien-bai-tap-002: bài của coach khác → 404
        if (await exercises.GetAsync(id, coach.Id, ct) is not { } exercise)
            return ExerciseErrors.NotFound;

        change(exercise);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return ExerciseErrors.VersionConflict; // bài bị sửa ở nơi khác giữa lúc đọc và lúc lưu
        }

        return await ExerciseCategoryCheck.DetailAsync(categories, exercise, ct);
    }
}
