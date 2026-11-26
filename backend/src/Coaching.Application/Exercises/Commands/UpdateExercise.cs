using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

public sealed class UpdateExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseCategoryRepository categories,
    IExerciseRepository exercises,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, SaveExerciseCommand command, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return ExerciseErrors.VersionRequired;

        // NFR-f03-thu-vien-bai-tap-002: bài không tồn tại hoặc của coach khác → 404
        if (await exercises.GetForUpdateAsync(id, coach.Id, version, ct) is not { } exercise)
            return ExerciseErrors.NotFound;

        var resolved = await ExerciseCategoryCheck.ResolveAsync(categories, coach.Id, command.MuscleGroupId, command.EquipmentId, ct);
        if (!resolved.IsSuccess)
            return resolved.Error!;
        var (muscleGroup, equipment) = resolved.Value;

        exercise.Rename(command.Name); // @BR-f03-thu-vien-bai-tap-002
        exercise.Categorise(muscleGroup.Id, equipment.Id);

        // @BR-f03-thu-vien-bai-tap-001: sửa thành (tên, dụng cụ) của bài KHÁC → 409; trùng chính nó (đổi hoa/thường) → lưu được
        if (await exercises.FindByKeyAsync(coach.Id, exercise.NameKey, exercise.EquipmentId, ct) is { } other && other.Id != exercise.Id)
            return ExerciseErrors.Duplicate(other.IsHidden ? other.Id : null);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return ExerciseErrors.VersionConflict; // @BR-f03-thu-vien-bai-tap-020
        }
        catch (UniqueConstraintException)
        {
            return ExerciseErrors.Duplicate(null); // @BR-f03-thu-vien-bai-tap-001: hai request đua nhau
        }

        // @BR-f03-thu-vien-bai-tap-021: chỉ bản trong thư viện đổi; giáo án (F05) giữ bản chụp
        return ExerciseDetailDto.From(exercise, muscleGroup.Name, equipment.Name);
    }
}
