using Coaching.Application.Exercises.Commands;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises;

internal static class ExerciseDetails
{
    /// <summary>
    /// Ghi phần chi tiết của bài từ thân POST/PUT (thay toàn bộ): video (@BR-f03-thu-vien-bai-tap-007), liều mặc định
    /// (@ADR-f03-thu-vien-bai-tap-006, BR-f03-thu-vien-bai-tap-009…012), hướng dẫn kỹ thuật/lỗi hay gặp (BR-f03-thu-vien-bai-tap-013, 014).
    /// Sai → DomainException → 400 kèm mã.
    /// </summary>
    public static void Apply(Exercise exercise, SaveExerciseCommand command)
    {
        exercise.SetVideo(command.VideoUrl);
        exercise.SetDose(ExerciseDose.Create(command.Sets, command.RepsMin, command.RepsMax, command.RirMin, command.RirMax,
            command.RestSeconds));
        exercise.SetNotes(command.TechniqueNotes, command.CommonMistakes);
    }
}
