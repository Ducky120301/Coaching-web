using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class RegistrationFormTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    private static RegistrationForm Create(RegistrationInput input) => RegistrationForm.Create(input, Today);

    private static string ErrorOf(RegistrationInput input) =>
        Assert.Throws<DomainException>(() => Create(input)).Code;

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Complete_form_has_no_missing_required() =>
        Assert.Empty(Create(RegistrationSamples.Complete()).MissingRequired());

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Missing_required_lists_exactly_the_empty_required_fields_in_form_order()
    {
        var input = RegistrationSamples.Complete() with
        {
            Pack = null, TrainingGoal = "", PhotoBackUrl = null, Availability = null, // Lịch rảnh không bắt buộc
        };

        Assert.Equal(new[] { "pack", "trainingGoal", "photoBackUrl" }, Create(input).MissingRequired());
    }

    // @BR-f02-ho-so-hoc-vien-004 (seed loại khỏi living doc): mục chỉ chứa khoảng trắng tính là thiếu
    [Fact]
    public void Whitespace_only_required_text_counts_as_missing()
    {
        var form = Create(RegistrationSamples.Complete() with { TrainingGoal = "   " });

        Assert.Null(form.TrainingGoal);
        Assert.Equal(new[] { "trainingGoal" }, form.MissingRequired());
    }

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Empty_form_misses_all_28_required_fields() =>
        Assert.Equal(28, RegistrationForm.CreateEmpty().MissingRequired().Count);

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(20)]
    [InlineData(72.5)]
    [InlineData(300)]
    public void Weight_inside_range_is_kept(double kg) =>
        Assert.Equal((decimal)kg, Create(new RegistrationInput { WeightKg = (decimal)kg }).WeightKg);

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(19.9)]
    [InlineData(300.1)]
    [InlineData(720)]
    public void Weight_outside_range_is_rejected(double kg) =>
        Assert.Equal("student.weightKg.out_of_range", ErrorOf(new RegistrationInput { WeightKg = (decimal)kg }));

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(3, true)]
    [InlineData(70, true)]
    [InlineData(2.9, false)]
    [InlineData(70.1, false)]
    public void Body_fat_must_be_between_3_and_70(double percent, bool valid)
    {
        var input = new RegistrationInput { BodyFatPercent = (decimal)percent };
        if (valid)
            Assert.Equal((decimal)percent, Create(input).BodyFatPercent);
        else
            Assert.Equal("student.bodyFatPercent.out_of_range", ErrorOf(input));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(10, true)]
    [InlineData(100, true)]
    [InlineData(9, false)]
    [InlineData(101, false)]
    public void Age_must_be_between_10_and_100(int age, bool valid)
    {
        var input = new RegistrationInput { Age = age };
        if (valid)
            Assert.Equal(age, Create(input).Age);
        else
            Assert.Equal("student.age.out_of_range", ErrorOf(input));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(100, true)]
    [InlineData(250, true)]
    [InlineData(99.9, false)]
    [InlineData(250.1, false)]
    public void Height_must_be_between_100_and_250(double cm, bool valid)
    {
        var input = new RegistrationInput { HeightCm = (decimal)cm };
        if (valid)
            Assert.Equal((decimal)cm, Create(input).HeightCm);
        else
            Assert.Equal("student.heightCm.out_of_range", ErrorOf(input));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Fact]
    public void Girths_and_pre_lifting_measurements_use_their_ranges()
    {
        var form = Create(new RegistrationInput { ChestCm = 10m, LeftThighCm = 250m, WeightBeforeLiftingKg = 20m, HeightThenCm = 250m });
        Assert.Equal(10m, form.ChestCm);
        Assert.Equal(250m, form.LeftThighCm);

        Assert.Equal("student.waistCm.out_of_range", ErrorOf(new RegistrationInput { WaistCm = 9.9m }));
        Assert.Equal("student.hipCm.out_of_range", ErrorOf(new RegistrationInput { HipCm = 250.1m }));
        Assert.Equal("student.weightBeforeLiftingKg.out_of_range", ErrorOf(new RegistrationInput { WeightBeforeLiftingKg = 19m }));
        Assert.Equal("student.heightThenCm.out_of_range", ErrorOf(new RegistrationInput { HeightThenCm = 251m }));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Fact]
    public void Price_starts_at_zero()
    {
        Assert.Equal(0, Create(new RegistrationInput { Price = 0 }).Price);
        Assert.Equal("student.price.out_of_range", ErrorOf(new RegistrationInput { Price = -1 }));
    }

    // @BR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Option_in_list_is_kept_trimmed() =>
        Assert.Equal("4 buổi", Create(new RegistrationInput { SessionsPerWeek = " 4 buổi " }).SessionsPerWeek);

    // @BR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Option_outside_list_is_rejected()
    {
        Assert.Equal("student.sessionsPerWeek.invalid_option", ErrorOf(new RegistrationInput { SessionsPerWeek = "8 buổi" }));
        Assert.Equal("student.stressLevel.invalid_option", ErrorOf(new RegistrationInput { StressLevel = "Stress-free" }));
    }

    // @BR-f02-ho-so-hoc-vien-010
    [Fact]
    public void Transfer_date_cannot_be_after_today()
    {
        Assert.Equal(Today, Create(new RegistrationInput { TransferDate = Today }).TransferDate);
        Assert.Equal("student.transferDate.in_future", ErrorOf(new RegistrationInput { TransferDate = Today.AddDays(1) }));
    }

    // @BR-f02-ho-so-hoc-vien-011
    [Theory]
    [InlineData("https://drive.google.com/open?id=abc")]
    [InlineData("http://example.com/anh.jpg")]
    public void Http_and_https_photo_links_are_kept(string url) =>
        Assert.Equal(url, Create(new RegistrationInput { PhotoFrontUrl = url }).PhotoFrontUrl);

    // @BR-f02-ho-so-hoc-vien-011 (seed "javascript:" loại khỏi living doc)
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("drive.google.com/abc")]
    [InlineData("ftp://example.com/anh.jpg")]
    public void Non_web_photo_links_are_rejected(string url) =>
        Assert.Equal("student.photoFrontUrl.invalid_url", ErrorOf(new RegistrationInput { PhotoFrontUrl = url }));

    // @ADR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Text_length_limits_apply()
    {
        Assert.Equal(200, Create(new RegistrationInput { Address = new string('a', 200) }).Address!.Length);
        Assert.Equal("student.address.too_long", ErrorOf(new RegistrationInput { Address = new string('a', 201) }));
        Assert.Equal(2000, Create(new RegistrationInput { TrainingGoal = new string('a', 2000) }).TrainingGoal!.Length);
        Assert.Equal("student.trainingGoal.too_long", ErrorOf(new RegistrationInput { TrainingGoal = new string('a', 2001) }));
        Assert.Equal("student.photoFrontUrl.too_long",
            ErrorOf(new RegistrationInput { PhotoFrontUrl = "https://x.vn/" + new string('a', 2048) }));
    }

    // @ADR-f02-ho-so-hoc-vien-002
    [Fact]
    public void ToInput_round_trips_every_field() =>
        Assert.Equal(RegistrationSamples.Complete(), Create(RegistrationSamples.Complete()).ToInput());

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Student_exposes_missing_required_of_its_form()
    {
        var at = new DateTimeOffset(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);
        var email = Email.Create("lan@gmail.com");

        Assert.Equal(28, Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", email, at).MissingRequiredFields.Count);
        Assert.Empty(Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", email, at,
            Create(RegistrationSamples.Complete())).MissingRequiredFields);
    }
}
