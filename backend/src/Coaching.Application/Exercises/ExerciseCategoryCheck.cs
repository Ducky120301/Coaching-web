using Coaching.Application.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises;

internal static class ExerciseCategoryCheck
{
    /// <summary>
    /// @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: thiếu nhóm cơ/dụng cụ → 400 riêng; mục không thuộc danh mục
    /// của coach (không có, của coach khác, hoặc sai loại) → 400 exercise.category_not_found.
    /// </summary>
    public static async Task<Result<(ExerciseCategory MuscleGroup, ExerciseCategory Equipment)>> ResolveAsync(
        IExerciseCategoryRepository categories, Guid coachId, Guid? muscleGroupId, Guid? equipmentId, CancellationToken ct)
    {
        if (muscleGroupId is not { } muscleId || muscleId == Guid.Empty)
            return ExerciseErrors.MuscleGroupRequired;
        if (equipmentId is not { } equipmentIdValue || equipmentIdValue == Guid.Empty)
            return ExerciseErrors.EquipmentRequired;

        var muscleGroup = await categories.GetAsync(muscleId, coachId, ct);
        var equipment = await categories.GetAsync(equipmentIdValue, coachId, ct);
        if (muscleGroup is not { Kind: CategoryKind.MuscleGroup } || equipment is not { Kind: CategoryKind.Equipment })
            return ExerciseErrors.CategoryNotFound;

        return Result<(ExerciseCategory MuscleGroup, ExerciseCategory Equipment)>.Success((muscleGroup, equipment));
    }

    /// <summary>Chi tiết bài kèm tên nhóm cơ/dụng cụ (mục danh mục không xoá được nên luôn có).</summary>
    public static async Task<ExerciseDetailDto> DetailAsync(IExerciseCategoryRepository categories, Exercise exercise,
        CancellationToken ct)
    {
        var muscleGroup = await categories.GetAsync(exercise.MuscleGroupId, exercise.CoachId, ct);
        var equipment = await categories.GetAsync(exercise.EquipmentId, exercise.CoachId, ct);
        return ExerciseDetailDto.From(exercise, muscleGroup!.Name, equipment!.Name);
    }
}
