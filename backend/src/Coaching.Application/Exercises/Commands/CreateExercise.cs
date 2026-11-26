using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

public sealed class CreateExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseCategoryRepository categories,
    IExerciseRepository exercises,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ExerciseDetailDto>> HandleAsync(SaveExerciseCommand command, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        var resolved = await ExerciseCategoryCheck.ResolveAsync(categories, coach.Id, command.MuscleGroupId, command.EquipmentId, ct);
        if (!resolved.IsSuccess)
            return resolved.Error!;
        var (muscleGroup, equipment) = resolved.Value;

        // @BR-f03-thu-vien-bai-tap-002 → 400 exercise.name_required / exercise.name_too_long
        var exercise = Exercise.Create(coach.Id, command.Name, muscleGroup.Id, equipment.Id);
        ExerciseDetails.Apply(exercise, command); // @BR-f03-thu-vien-bai-tap-007…014

        // @BR-f03-thu-vien-bai-tap-001: trùng (tên chuẩn hoá, dụng cụ) kể cả bài đang ẩn → 409; bài trùng đang ẩn → gợi ý bỏ ẩn
        if (await exercises.FindByKeyAsync(coach.Id, exercise.NameKey, equipment.Id, ct) is { } existing)
            return ExerciseErrors.Duplicate(existing.IsHidden ? existing.Id : null);

        exercises.Add(exercise);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            return ExerciseErrors.Duplicate(null); // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: hai request đua nhau
        }

        return ExerciseDetailDto.From(exercise, muscleGroup.Name, equipment.Name);
    }
}
