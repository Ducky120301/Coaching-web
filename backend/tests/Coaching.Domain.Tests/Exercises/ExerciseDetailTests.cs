using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseDetailTests
{
    private static Exercise Create() => Exercise.Create(Guid.NewGuid(), "Lateral Raise", Guid.NewGuid(), Guid.NewGuid());

    private static string ErrorOf(Action action) => Assert.Throws<DomainException>(action).Code;

    // @BR-f03-thu-vien-bai-tap-008: có link → không thiếu video; bỏ link → thiếu video
    [Fact]
    public void BR008_Exercise_without_link_is_missing_video()
    {
        var exercise = Create();
        Assert.False(exercise.HasVideo);

        exercise.SetVideo("https://www.youtube.com/watch?v=Nhvz9EzdJ4U");
        Assert.True(exercise.HasVideo);
        Assert.Equal("https://www.youtube.com/watch?v=Nhvz9EzdJ4U", exercise.VideoUrl);

        exercise.SetVideo("  ");
        Assert.False(exercise.HasVideo);
        Assert.Null(exercise.VideoUrl);
    }

    // @BR-f03-thu-vien-bai-tap-007: link sai bị từ chối, video cũ giữ nguyên
    [Fact]
    public void BR007_Invalid_link_keeps_the_previous_video()
    {
        var exercise = Create();
        exercise.SetVideo("https://youtu.be/abc123XYZ09");

        Assert.Equal("exercise.video_not_youtube", ErrorOf(() => exercise.SetVideo("https://vimeo.com/123")));

        Assert.Equal("https://youtu.be/abc123XYZ09", exercise.VideoUrl);
    }

    // @ADR-f03-thu-vien-bai-tap-006
    [Fact]
    public void ADR006_SetDose_replaces_the_default_dose()
    {
        var exercise = Create();
        var dose = ExerciseDose.Create(3, 10, 12, 1, 2, 90);

        exercise.SetDose(dose);

        Assert.Equal(dose, exercise.Dose);
    }

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014: không bắt buộc (trống → null), lưu đã trim, tối đa 2000 ký tự
    [Fact]
    public void BR013_BR014_Notes_are_optional_trimmed_and_at_most_2000_characters()
    {
        var exercise = Create();

        exercise.SetNotes("  Giữ khuỷu hơi cong, kéo bằng vai ", "Nhún vai khi kéo");
        Assert.Equal("Giữ khuỷu hơi cong, kéo bằng vai", exercise.TechniqueNotes);
        Assert.Equal("Nhún vai khi kéo", exercise.CommonMistakes);

        exercise.SetNotes(new string('a', 2000), "   ");
        Assert.Equal(2000, exercise.TechniqueNotes!.Length);
        Assert.Null(exercise.CommonMistakes);
    }

    // @BR-f03-thu-vien-bai-tap-013: 2001 ký tự → không lưu
    [Fact]
    public void BR013_Technique_notes_of_2001_characters_are_rejected() =>
        Assert.Equal("exercise.notes_too_long", ErrorOf(() => Create().SetNotes(new string('a', 2001), null)));

    // @BR-f03-thu-vien-bai-tap-014: 2001 ký tự → không lưu
    [Fact]
    public void BR014_Common_mistakes_of_2001_characters_are_rejected() =>
        Assert.Equal("exercise.notes_too_long", ErrorOf(() => Create().SetNotes(null, new string('a', 2001))));
}
