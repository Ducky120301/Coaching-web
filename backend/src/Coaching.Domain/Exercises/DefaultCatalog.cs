namespace Coaching.Domain.Exercises;

/// <summary>@BR-f03-thu-vien-bai-tap-005: danh mục ban đầu của mọi coach, đúng thứ tự spec (thứ tự hiển thị = SortOrder).</summary>
public static class DefaultCatalog
{
    public static IReadOnlyList<string> MuscleGroups { get; } =
    [
        "Ngực", "Vai", "Tay sau", "Tay trước", "Lưng", "Vai sau", "Đùi trước", "Đùi sau", "Mông", "Bắp chân", "Bụng",
    ];

    public static IReadOnlyList<string> Equipment { get; } =
    [
        "Machine", "Cable", "Dumbbell", "Barbell", "Smith Machine", "Kettlebell", "Bodyweight",
    ];
}
