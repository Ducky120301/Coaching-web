using Coaching.Application.Common;

namespace Coaching.Application.Exercises;

/// <summary>Lỗi của thư viện bài tập và danh mục (mã + câu chữ theo bảng API của tech-design F03).</summary>
public static class ExerciseErrors
{
    // @BR-f03-thu-vien-bai-tap-006
    public static readonly Error CategoryDuplicate =
        new("category.duplicate", "Đã có mục cùng tên.", ErrorKind.Conflict);

    public static readonly Error CategoryKindInvalid =
        new("category.kind_invalid", "Loại danh mục phải là MuscleGroup hoặc Equipment.");

    public static readonly Error NotFound =
        new("exercise.not_found", "Không tìm thấy bài tập.", ErrorKind.NotFound);

    // @BR-f03-thu-vien-bai-tap-001: bài trùng đang ẩn → kèm Id để web admin gợi ý "Bỏ ẩn bài đó"
    public static Error Duplicate(Guid? hiddenExerciseId) =>
        new("exercise.duplicate", "Đã có bài cùng tên và dụng cụ.", ErrorKind.Conflict,
            hiddenExerciseId is { } id ? new Dictionary<string, object?> { ["hiddenExerciseId"] = id } : null);

    public static readonly Error VersionRequired =
        new("exercise.version_required", "Thiếu phiên bản bài tập. Tải lại bài rồi thử lại.");

    // @BR-f03-thu-vien-bai-tap-020
    public static readonly Error VersionConflict =
        new("exercise.version_conflict", "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.", ErrorKind.Conflict);

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    public static readonly Error CategoryNotFound =
        new("exercise.category_not_found", "Nhóm cơ hoặc dụng cụ không có trong danh mục.");

    // @BR-f03-thu-vien-bai-tap-003
    public static readonly Error MuscleGroupRequired = new("exercise.muscle_group_required", "Chọn nhóm cơ.");

    // @BR-f03-thu-vien-bai-tap-004
    public static readonly Error EquipmentRequired = new("exercise.equipment_required", "Chọn dụng cụ.");

    // @BR-f03-thu-vien-bai-tap-017
    public static readonly Error InUse =
        new("exercise.in_use", "Bài đang có trong giáo án, chỉ ẩn được.", ErrorKind.Conflict);
}
