using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseVisibilityTests
{
    // @BR-f03-thu-vien-bai-tap-015: ẩn và bỏ ẩn được bất kỳ bài nào; lặp lại không lỗi; nội dung bài không đổi
    [Fact]
    public void BR015_Hide_and_unhide_any_exercise()
    {
        var exercise = Exercise.Create(Guid.NewGuid(), "Chest Dip", Guid.NewGuid(), Guid.NewGuid());

        exercise.Hide();
        exercise.Hide();
        Assert.True(exercise.IsHidden);
        Assert.Equal("Chest Dip", exercise.Name);

        exercise.Unhide();
        exercise.Unhide();
        Assert.False(exercise.IsHidden);
    }
}
