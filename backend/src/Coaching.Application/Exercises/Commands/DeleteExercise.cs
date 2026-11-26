using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

public sealed class DeleteExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseUsage usage,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return Result.Failure(StudentErrors.CoachNotFound);
        if (await exercises.GetAsync(id, coach.Id, ct) is not { } exercise)
            return Result.Failure(ExerciseErrors.NotFound); // NFR-f03-thu-vien-bai-tap-002

        // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004: bài đang có trong giáo án chỉ ẩn được
        if (await usage.IsUsedAsync(exercise.Id, ct))
            return Result.Failure(ExerciseErrors.InUse);

        // @BR-f03-thu-vien-bai-tap-017: xoá thật → cặp (tên, dụng cụ) rời chỉ mục duy nhất, thêm lại được
        exercises.Remove(exercise);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(ExerciseErrors.VersionConflict);
        }
        return Result.Success();
    }
}
