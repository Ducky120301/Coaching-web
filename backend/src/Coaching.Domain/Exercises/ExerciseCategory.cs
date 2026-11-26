using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-003: một mục danh mục (nhóm cơ hoặc dụng cụ) của coach. Duy nhất theo (CoachId, Kind, NameKey).
/// MVP chỉ thêm mới — không đổi tên, không xoá (spec §5).
/// </summary>
public sealed class ExerciseCategory : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int NameKeyMaxLength = 200;

    public Guid CoachId { get; private set; }

    public CategoryKind Kind { get; private set; }

    public string Name { get; private set; } = default!;

    /// <summary>@ADR-f03-thu-vien-bai-tap-002: khoá so trùng (ExerciseNameKey).</summary>
    public string NameKey { get; private set; } = default!;

    public int SortOrder { get; private set; }

    private ExerciseCategory() { }

    private ExerciseCategory(Guid id) : base(id) { }

    // @BR-f03-thu-vien-bai-tap-006: tên bắt buộc (sau trim), tối đa 100 ký tự
    public static ExerciseCategory Create(Guid coachId, CategoryKind kind, string? name, int sortOrder)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException("category.name_required", "Nhập tên mục.");
        if (trimmed.Length > NameMaxLength)
            throw new DomainException("category.name_too_long", $"Tên mục tối đa {NameMaxLength} ký tự.");

        return new ExerciseCategory(Guid.CreateVersion7())
        {
            CoachId = coachId,
            Kind = kind,
            Name = trimmed,
            NameKey = ExerciseNameKey.For(trimmed),
            SortOrder = sortOrder,
        };
    }
}
