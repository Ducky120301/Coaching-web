using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-001: một bài trong thư viện của coach. Duy nhất theo (CoachId, NameKey, EquipmentId), kể cả
/// bài đang ẩn (chỉ mục duy nhất trong DB). Tham chiếu danh mục chỉ bằng Id.
/// @BR-f03-thu-vien-bai-tap-021: giáo án (F05) lưu bản chụp bài lúc thêm — sửa ở đây chỉ đổi bản trong thư viện.
/// </summary>
public sealed class Exercise : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int NotesMaxLength = 2000;
    public const int KeyMaxLength = 200;
    public const int VideoUrlMaxLength = 2048;

    public Guid CoachId { get; private set; }

    public string Name { get; private set; } = default!;

    /// <summary>@ADR-f03-thu-vien-bai-tap-002: khoá so trùng — chỉ aggregate ghi.</summary>
    public string NameKey { get; private set; } = default!;

    /// <summary>@ADR-f03-thu-vien-bai-tap-002: khoá tìm kiếm, cùng chuẩn hoá với NameKey.</summary>
    public string SearchKey { get; private set; } = default!;

    public Guid MuscleGroupId { get; private set; }

    public Guid EquipmentId { get; private set; }

    public string? VideoUrl { get; private set; }

    public bool HasVideo => VideoUrl is not null;

    public ExerciseDose Dose { get; private set; } = ExerciseDose.Empty;

    public string? TechniqueNotes { get; private set; }

    public string? CommonMistakes { get; private set; }

    public bool IsHidden { get; private set; }

    /// <summary>@BR-f03-thu-vien-bai-tap-020: rowversion do SQL Server sinh, dùng chặn ghi đè.</summary>
    public byte[] Version { get; private set; } = [];

    private Exercise() { }

    private Exercise(Guid id) : base(id) { }

    public static Exercise Create(Guid coachId, string? name, Guid muscleGroupId, Guid equipmentId)
    {
        if (coachId == Guid.Empty)
            throw new DomainException("exercise.coach_required", "Bài tập phải thuộc một coach.");

        var exercise = new Exercise(Guid.CreateVersion7()) { CoachId = coachId };
        exercise.Rename(name);
        exercise.Categorise(muscleGroupId, equipmentId);
        return exercise;
    }

    // @BR-f03-thu-vien-bai-tap-002: bắt buộc, ≤ 100 ký tự sau trim
    // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-002: khoá trùng/tìm kiếm luôn đi theo tên
    public void Rename(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException("exercise.name_required", "Nhập tên bài.");
        if (trimmed.Length > NameMaxLength)
            throw new DomainException("exercise.name_too_long", $"Tên bài tối đa {NameMaxLength} ký tự.");

        Name = trimmed;
        NameKey = ExerciseNameKey.For(trimmed);
        SearchKey = NameKey;
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: đúng một nhóm cơ, đúng một dụng cụ (thuộc danh mục: kiểm ở Application)
    public void Categorise(Guid muscleGroupId, Guid equipmentId)
    {
        if (muscleGroupId == Guid.Empty)
            throw new DomainException("exercise.muscle_group_required", "Chọn nhóm cơ.");
        if (equipmentId == Guid.Empty)
            throw new DomainException("exercise.equipment_required", "Chọn dụng cụ.");

        MuscleGroupId = muscleGroupId;
        EquipmentId = equipmentId;
    }

    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: chỉ link YouTube; trống → không có video
    // @BR-f03-thu-vien-bai-tap-008: không có link = thiếu video (HasVideo)
    public void SetVideo(string? url) => VideoUrl = YouTubeUrl.Normalize(url);

    // @ADR-f03-thu-vien-bai-tap-006: liều đã kiểm bởi ExerciseDose.Create (BR-f03-thu-vien-bai-tap-009…012)
    public void SetDose(ExerciseDose dose) => Dose = dose ?? throw new ArgumentNullException(nameof(dose));

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014: không bắt buộc, ≤ 2000 ký tự; kiểm cả hai trước khi ghi
    public void SetNotes(string? technique, string? mistakes)
    {
        var techniqueNotes = Notes(technique, "Hướng dẫn kỹ thuật");
        var commonMistakes = Notes(mistakes, "Lỗi hay gặp");
        TechniqueNotes = techniqueNotes;
        CommonMistakes = commonMistakes;
    }

    private static string? Notes(string? text, string label)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var trimmed = text.Trim();
        if (trimmed.Length > NotesMaxLength)
            throw new DomainException("exercise.notes_too_long", $"{label} tối đa {NotesMaxLength} ký tự.");
        return trimmed;
    }

    // @BR-f03-thu-vien-bai-tap-015: coach ẩn/bỏ ẩn được bất kỳ bài nào của mình; lặp lại không lỗi
    public void Hide() => IsHidden = true;

    // @BR-f03-thu-vien-bai-tap-015
    public void Unhide() => IsHidden = false;
}
