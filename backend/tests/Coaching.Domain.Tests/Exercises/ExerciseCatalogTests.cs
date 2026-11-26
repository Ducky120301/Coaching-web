using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseCatalogTests
{
    private static readonly Guid CoachId = Guid.NewGuid();

    private static ExerciseCategory Category(string? name, CategoryKind kind = CategoryKind.MuscleGroup) =>
        ExerciseCategory.Create(CoachId, kind, name, 1);

    // @BR-f03-thu-vien-bai-tap-005: 11 nhóm cơ và 7 dụng cụ, đúng thứ tự của spec
    [Fact]
    public void BR005_Default_catalog_has_11_muscle_groups_and_7_equipment_in_spec_order()
    {
        Assert.Equal(
            new[] { "Ngực", "Vai", "Tay sau", "Tay trước", "Lưng", "Vai sau", "Đùi trước", "Đùi sau", "Mông", "Bắp chân", "Bụng" },
            DefaultCatalog.MuscleGroups.ToArray());
        Assert.Equal(
            new[] { "Machine", "Cable", "Dumbbell", "Barbell", "Smith Machine", "Kettlebell", "Bodyweight" },
            DefaultCatalog.Equipment.ToArray());
    }

    // @ADR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-006: hoa/thường, dấu, khoảng trắng thừa, kiểu dấu gạch không làm khác khoá
    [Theory]
    [InlineData("Lateral Raise", "lateral raise")]
    [InlineData("  lateral   raise  ", "lateral raise")]
    [InlineData("Ép ngực", "ep nguc")]
    [InlineData("Ep nguc", "ep nguc")]
    [InlineData("Đùi trước", "dui truoc")]
    [InlineData("T–Bar Row", "t-bar row")]
    [InlineData("T—Bar Row", "t-bar row")]
    [InlineData("T‐Bar Row", "t-bar row")]
    [InlineData("T-Bar Row", "t-bar row")]
    [InlineData("Lat\tPulldown", "lat pulldown")]
    [InlineData("45° Incline Chest Fly", "45° incline chest fly")]
    public void ADR002_Name_key_ignores_case_marks_extra_spaces_and_dash_style(string name, string key) =>
        Assert.Equal(key, ExerciseNameKey.For(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ADR002_Name_key_of_blank_is_empty(string? name) => Assert.Equal(string.Empty, ExerciseNameKey.For(name));

    // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-003
    [Fact]
    public void BR006_Category_keeps_trimmed_name_kind_order_and_key()
    {
        var category = ExerciseCategory.Create(CoachId, CategoryKind.Equipment, "  Resistance   Band ", 8);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal(CoachId, category.CoachId);
        Assert.Equal(CategoryKind.Equipment, category.Kind);
        Assert.Equal("Resistance   Band", category.Name);
        Assert.Equal("resistance band", category.NameKey);
        Assert.Equal(8, category.SortOrder);
    }

    // @BR-f03-thu-vien-bai-tap-006: "ngực" trùng "Ngực" (so theo khoá)
    [Fact]
    public void BR006_Names_differing_only_by_case_marks_or_spaces_share_a_key() =>
        Assert.Equal(Category("Ngực").NameKey, Category("  NGUC ").NameKey);

    // @BR-f03-thu-vien-bai-tap-006: tên danh mục trống → không thêm
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BR006_Blank_category_name_is_rejected(string? name) =>
        Assert.Equal("category.name_required", Assert.Throws<DomainException>(() => Category(name)).Code);

    [Fact]
    public void Category_name_is_at_most_100_characters()
    {
        Assert.Equal(100, Category(new string('a', 100)).Name.Length);
        Assert.Equal("category.name_too_long",
            Assert.Throws<DomainException>(() => Category(new string('a', 101))).Code);
    }
}
