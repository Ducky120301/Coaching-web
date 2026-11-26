namespace Coaching.Domain.Exercises;

/// <summary>
/// @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019: điều kiện danh sách bài.
/// <paramref name="NormalizedQuery"/> đã qua ExerciseNameKey.For (rỗng = không lọc tên); <paramref name="Hidden"/> = true chỉ bài đã ẩn.
/// </summary>
public sealed record ExerciseFilter(string NormalizedQuery, Guid? MuscleGroupId, Guid? EquipmentId, bool Hidden);

public sealed record ExerciseListRow(Guid Id, string Name, Guid MuscleGroupId, Guid EquipmentId, bool HasVideo, bool IsHidden);
