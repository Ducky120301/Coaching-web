namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-006: liều mặc định có cấu trúc (Sets, Reps thấp–cao, RIR thấp–cao, nghỉ giây); mọi phần không
/// bắt buộc. Một số đơn lẻ lưu là thấp = cao. Kiểm khoảng ở <c>Create</c> (Task 3).
/// </summary>
public sealed record ExerciseDose(int? Sets, int? RepsMin, int? RepsMax, int? RirMin, int? RirMax, int? RestSeconds)
{
    public static readonly ExerciseDose Empty = new(null, null, null, null, null, null);
}
