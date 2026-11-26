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
}
