using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseTests
{
    private static readonly Guid CoachId = Guid.NewGuid();
    private static readonly Guid Chest = Guid.NewGuid();
    private static readonly Guid Shoulder = Guid.NewGuid();
    private static readonly Guid Cable = Guid.NewGuid();
    private static readonly Guid Dumbbell = Guid.NewGuid();

    private static Exercise Create(string? name = "Lateral Raise") => Exercise.Create(CoachId, name, Chest, Cable);

    private static string ErrorOf(Action action) => Assert.Throws<DomainException>(action).Code;

    // @ADR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002: tên đã trim; NameKey/SearchKey theo ExerciseNameKey; bài mới chưa ẩn, chưa có video/liều/hướng dẫn
    [Fact]
    public void ADR001_Create_keeps_trimmed_name_categories_and_keys()
    {
        var exercise = Create("  Lateral   Raise ");

        Assert.NotEqual(Guid.Empty, exercise.Id);
        Assert.Equal(CoachId, exercise.CoachId);
        Assert.Equal("Lateral   Raise", exercise.Name);
        Assert.Equal("lateral raise", exercise.NameKey);
        Assert.Equal(exercise.NameKey, exercise.SearchKey);
        Assert.Equal(Chest, exercise.MuscleGroupId);
        Assert.Equal(Cable, exercise.EquipmentId);
        Assert.False(exercise.IsHidden);
        Assert.Null(exercise.VideoUrl);
        Assert.False(exercise.HasVideo);
        Assert.Equal(ExerciseDose.Empty, exercise.Dose);
        Assert.Null(exercise.TechniqueNotes);
        Assert.Null(exercise.CommonMistakes);
    }

    // @BR-f03-thu-vien-bai-tap-002: tên trống hoặc chỉ có khoảng trắng → báo thiếu tên
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BR002_Name_is_required(string? name) => Assert.Equal("exercise.name_required", ErrorOf(() => Create(name)));

    // @BR-f03-thu-vien-bai-tap-002: tối đa 100 ký tự sau trim; 101 → không lưu
    [Fact]
    public void BR002_Name_is_at_most_100_characters_after_trim()
    {
        Assert.Equal(100, Create($"  {new string('a', 100)}  ").Name.Length);
        Assert.Equal("exercise.name_too_long", ErrorOf(() => Create(new string('a', 101))));
    }

    // @BR-f03-thu-vien-bai-tap-003: không có nhóm cơ → báo thiếu nhóm cơ
    [Fact]
    public void BR003_Muscle_group_is_required() =>
        Assert.Equal("exercise.muscle_group_required", ErrorOf(() => Exercise.Create(CoachId, "Chest Press", Guid.Empty, Cable)));

    // @BR-f03-thu-vien-bai-tap-004: không có dụng cụ → báo thiếu dụng cụ
    [Fact]
    public void BR004_Equipment_is_required() =>
        Assert.Equal("exercise.equipment_required", ErrorOf(() => Exercise.Create(CoachId, "Cable Crossover", Chest, Guid.Empty)));

    // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-002: hoa/thường, dấu, khoảng trắng thừa, kiểu dấu gạch → cùng khoá
    [Theory]
    [InlineData("Lateral Raise", "  lateral   raise ")]
    [InlineData("Ép ngực", "Ep nguc")]
    [InlineData("T–Bar Row", "T-Bar Row")]
    public void BR001_Names_written_differently_share_a_key(string existing, string added) =>
        Assert.Equal(Create(existing).NameKey, Create(added).NameKey);

    // @BR-f03-thu-vien-bai-tap-001: sửa chữ hoa/thường tên của chính bài đó — khoá không đổi
    [Fact]
    public void BR001_Rename_updates_name_and_keeps_key_when_only_case_changes()
    {
        var exercise = Create("lateral raise");

        exercise.Rename("Lateral Raise");

        Assert.Equal("Lateral Raise", exercise.Name);
        Assert.Equal("lateral raise", exercise.NameKey);
        Assert.Equal("lateral raise", exercise.SearchKey);
    }

    // @BR-f03-thu-vien-bai-tap-002: đổi sang tên trống bị từ chối, bài giữ tên cũ
    [Fact]
    public void BR002_Rename_to_blank_is_rejected_and_keeps_old_name()
    {
        var exercise = Create("Chest Press");

        Assert.Equal("exercise.name_required", ErrorOf(() => exercise.Rename("  ")));

        Assert.Equal("Chest Press", exercise.Name);
        Assert.Equal("chest press", exercise.NameKey);
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [Fact]
    public void Categorise_changes_both_categories_or_nothing()
    {
        var exercise = Create();

        exercise.Categorise(Shoulder, Dumbbell);
        Assert.Equal((Shoulder, Dumbbell), (exercise.MuscleGroupId, exercise.EquipmentId));

        Assert.Equal("exercise.equipment_required", ErrorOf(() => exercise.Categorise(Chest, Guid.Empty)));
        Assert.Equal((Shoulder, Dumbbell), (exercise.MuscleGroupId, exercise.EquipmentId));
    }

    // @BR-f03-thu-vien-bai-tap-021: sửa bài chỉ đổi bản trong thư viện (giáo án F05 lưu bản chụp, không tham chiếu sống)
    [Fact]
    public void BR021_Editing_changes_the_library_copy()
    {
        var exercise = Create("Lateral Raise");

        exercise.Rename("Cable Lateral Raise");

        Assert.Equal("Cable Lateral Raise", exercise.Name);
        Assert.Equal("cable lateral raise", exercise.NameKey);
    }
}
