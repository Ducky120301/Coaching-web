using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class RegistrationGoogleFormTests
{
    private static readonly DateOnly Today = new(2026, 11, 26);

    private static RegistrationForm Create(RegistrationInput input) => RegistrationForm.Create(input, Today);

    private static string ErrorOf(RegistrationInput input) =>
        Assert.Throws<DomainException>(() => Create(input)).Code;

    // @BR-f02-import-excel-001 @ADR-f02-import-excel-001: form không còn Lịch rảnh, Dụng cụ
    [Fact]
    public void Form_has_no_availability_or_equipment()
    {
        foreach (var name in new[] { "Availability", "Equipment" })
        {
            Assert.Null(typeof(RegistrationInput).GetProperty(name));
            Assert.Null(typeof(RegistrationForm).GetProperty(name));
        }
        Assert.DoesNotContain(RegistrationFields.All, f => f.Code is "availability" or "equipment");
    }

    // @ADR-f02-import-excel-001: 4 ảnh + 2 mục số + mỡ/cách đo cũ đã gộp
    [Fact]
    public void Merged_google_form_questions_replace_old_fields()
    {
        string[] removed =
        [
            "BodyFatPercent", "BodyFatMethod", "WeightBeforeLiftingKg", "HeightThenCm",
            "PhotoFrontUrl", "PhotoBackUrl", "PhotoLeftUrl", "PhotoRightUrl",
        ];
        foreach (var name in removed)
            Assert.Null(typeof(RegistrationInput).GetProperty(name));

        Assert.Equal("Mỡ cơ thể và cách đo", RegistrationFields.LabelOf("bodyFatAndMethod"));
        Assert.Equal("Cân nặng và chiều cao trước khi tập tạ", RegistrationFields.LabelOf("weightHeightBeforeLifting"));
        Assert.Equal("Link ảnh toàn thân", RegistrationFields.LabelOf("bodyPhotoUrl"));
    }

    // @BR-f02-import-excel-003
    [Theory]
    [InlineData("1")]
    [InlineData("Khoảng 2 năm rưỡi")]
    [InlineData("1–2 năm")] // giá trị của hồ sơ cũ vẫn giữ nguyên
    public void Training_years_is_free_text_kept_verbatim(string text) =>
        Assert.Equal(text, Create(new RegistrationInput { TrainingYears = text }).TrainingYears);

    // @BR-f02-import-excel-003 (seed ❌ 201 ký tự)
    [Fact]
    public void Training_years_is_at_most_200_characters()
    {
        Assert.Equal(200, Create(new RegistrationInput { TrainingYears = new string('a', 200) }).TrainingYears!.Length);
        Assert.Equal("student.trainingYears.too_long",
            ErrorOf(new RegistrationInput { TrainingYears = new string('a', 201) }));
    }

    // @BR-f02-import-excel-004
    [Theory]
    [InlineData("34")]
    [InlineData("18% đo InBody")]
    public void Body_fat_and_method_is_free_text_kept_verbatim(string text) =>
        Assert.Equal(text, Create(new RegistrationInput { BodyFatAndMethod = text }).BodyFatAndMethod);

    // @BR-f02-import-excel-004 (seed ❌ 201 ký tự)
    [Fact]
    public void Body_fat_and_method_is_at_most_200_characters()
    {
        Assert.Equal(200, Create(new RegistrationInput { BodyFatAndMethod = new string('a', 200) }).BodyFatAndMethod!.Length);
        Assert.Equal("student.bodyFatAndMethod.too_long",
            ErrorOf(new RegistrationInput { BodyFatAndMethod = new string('a', 201) }));
    }

    // @BR-f02-import-excel-005
    [Fact]
    public void Weight_and_height_before_lifting_is_a_paragraph_of_at_most_2000_characters()
    {
        Assert.Equal("60kg, 1m70",
            Create(new RegistrationInput { WeightHeightBeforeLifting = "60kg, 1m70" }).WeightHeightBeforeLifting);
        Assert.Equal(2000,
            Create(new RegistrationInput { WeightHeightBeforeLifting = new string('a', 2000) }).WeightHeightBeforeLifting!.Length);
        Assert.Equal("student.weightHeightBeforeLifting.too_long",
            ErrorOf(new RegistrationInput { WeightHeightBeforeLifting = new string('a', 2001) }));
    }

    // @BR-f02-import-excel-006
    [Theory]
    [InlineData("https://drive.google.com/open?id=abc")]
    [InlineData("http://example.com/anh.jpg")]
    public void Body_photo_url_accepts_web_links(string url) =>
        Assert.Equal(url, Create(new RegistrationInput { BodyPhotoUrl = url }).BodyPhotoUrl);

    // @BR-f02-import-excel-006
    [Theory]
    [InlineData("drive.google.com/abc")]
    [InlineData("ftp://x.vn/a.jpg")]
    [InlineData("javascript:alert(1)")]
    public void Body_photo_url_rejects_non_web_links(string url) =>
        Assert.Equal("student.bodyPhotoUrl.invalid_url", ErrorOf(new RegistrationInput { BodyPhotoUrl = url }));

    // @BR-f02-import-excel-006: tối đa 2048 ký tự
    [Fact]
    public void Body_photo_url_is_at_most_2048_characters()
    {
        var max = "https://x.vn/" + new string('a', 2048 - "https://x.vn/".Length);
        Assert.Equal(max, Create(new RegistrationInput { BodyPhotoUrl = max }).BodyPhotoUrl);
        Assert.Equal("student.bodyPhotoUrl.too_long", ErrorOf(new RegistrationInput { BodyPhotoUrl = max + "a" }));
    }

    // @BR-f02-import-excel-007
    [Fact]
    public void Required_fields_follow_google_form()
    {
        var required = RegistrationFields.All.Where(f => f.Required).Select(f => f.Code).ToHashSet();

        Assert.Contains("trainingYears", required);
        Assert.Contains("weightHeightBeforeLifting", required);
        Assert.Contains("bodyPhotoUrl", required);
        Assert.DoesNotContain("bodyFatAndMethod", required);
        Assert.Contains(RegistrationFields.All, f => f.Code == "bodyFatAndMethod");
        Assert.Equal(24, required.Count);
    }

    // @BR-f02-import-excel-007
    [Fact]
    public void Empty_body_photo_url_is_reported_missing() =>
        Assert.Equal(new[] { "bodyPhotoUrl" },
            Create(RegistrationSamples.Complete() with { BodyPhotoUrl = null }).MissingRequired());

    // @BR-f02-import-excel-007
    [Fact]
    public void Empty_body_fat_and_method_is_not_missing() =>
        Assert.Empty(Create(RegistrationSamples.Complete() with { BodyFatAndMethod = "  " }).MissingRequired());

    // @ADR-f02-import-excel-003: Validate gom mọi lỗi theo thứ tự form, lý do không kèm tên mục
    [Fact]
    public void Validate_collects_every_error_in_form_order()
    {
        var input = new RegistrationInput
        {
            TransferDate = Today.AddDays(1),
            Address = new string('a', 201),
            Age = 5,
            HeightCm = 300m,
            StressLevel = "Cao quá",
            BodyPhotoUrl = "drive.google.com/abc",
        };

        var (form, errors) = RegistrationForm.Validate(input, Today);

        Assert.Null(form);
        Assert.Equal(
            new[]
            {
                new RegistrationFieldError("transferDate", "không được sau hôm nay"),
                new RegistrationFieldError("address", "tối đa 200 ký tự"),
                new RegistrationFieldError("age", "phải từ 10 đến 100"),
                new RegistrationFieldError("heightCm", "phải từ 100 đến 250"),
                new RegistrationFieldError("stressLevel", "không thuộc danh sách"),
                new RegistrationFieldError("bodyPhotoUrl", "link ảnh không hợp lệ"),
            },
            errors);
    }

    // @ADR-f02-import-excel-003
    [Fact]
    public void Validate_returns_the_form_when_everything_is_valid()
    {
        var (form, errors) = RegistrationForm.Validate(RegistrationSamples.Complete(), Today);

        Assert.Empty(errors);
        Assert.NotNull(form);
        Assert.Equal(RegistrationSamples.Complete(), form.ToInput());
    }

    // @ADR-f02-import-excel-003: giá dưới 0 có lý do riêng
    [Fact]
    public void Validate_reports_negative_price()
    {
        var (_, errors) = RegistrationForm.Validate(new RegistrationInput { Price = -1 }, Today);

        Assert.Equal(new[] { new RegistrationFieldError("price", "phải từ 0 đồng") }, errors);
    }

    // @ADR-f02-import-excel-003 @ADR-f02-ho-so-hoc-vien-006: Create vẫn ném lỗi đầu tiên, mã + câu chữ như trước
    [Fact]
    public void Create_still_throws_the_first_error_with_unchanged_code_and_message()
    {
        var ex = Assert.Throws<DomainException>(() => Create(new RegistrationInput { Age = 5, HeightCm = 300m }));
        Assert.Equal("student.age.out_of_range", ex.Code);
        Assert.Equal("Tuổi phải từ 10 đến 100.", ex.Message);

        var option = Assert.Throws<DomainException>(() => Create(new RegistrationInput { StressLevel = "Cao quá" }));
        Assert.Equal("student.stressLevel.invalid_option", option.Code);
        Assert.Equal("Mức độ căng thẳng không có lựa chọn \"Cao quá\".", option.Message);

        var date = Assert.Throws<DomainException>(() => Create(new RegistrationInput { TransferDate = Today.AddDays(1) }));
        Assert.Equal("student.transferDate.in_future", date.Code);
        Assert.Equal("Ngày chuyển khoản không được sau ngày hôm nay.", date.Message);

        var url = Assert.Throws<DomainException>(() => Create(new RegistrationInput { BodyPhotoUrl = "ftp://x.vn/a.jpg" }));
        Assert.Equal("Link ảnh toàn thân phải là địa chỉ web bắt đầu bằng http:// hoặc https://.", url.Message);
    }
}
