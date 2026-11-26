namespace Coaching.Application.Exercises.Commands;

/// <summary>
/// Thân POST/PUT bài tập (thay toàn bộ nội dung bài). @ADR-f03-thu-vien-bai-tap-006: liều có cấu trúc — một số đơn lẻ
/// gửi min = max. <paramref name="Version"/> bắt buộc khi sửa, bị bỏ qua khi thêm.
/// </summary>
public sealed record SaveExerciseCommand(
    string? Name,
    Guid? MuscleGroupId,
    Guid? EquipmentId,
    string? VideoUrl,
    int? Sets,
    int? RepsMin,
    int? RepsMax,
    int? RirMin,
    int? RirMax,
    int? RestSeconds,
    string? TechniqueNotes,
    string? CommonMistakes,
    string? Version);
