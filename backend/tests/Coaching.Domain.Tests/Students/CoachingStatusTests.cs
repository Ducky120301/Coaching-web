using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class CoachingStatusTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At = new(2026, 11, 25, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Actor = Guid.NewGuid();

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan",
        Email.Create("lan@gmail.com"), CreatedAt, RegistrationForm.Create(new RegistrationInput { TrainingGoal = "Giảm mỡ, giữ cơ" },
            new DateOnly(2026, 11, 20)));

    // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-020 @ADR-f02-ho-so-hoc-vien-005
    [Fact]
    public void Change_adds_exactly_one_history_row_with_from_to_actor_time_reason()
    {
        var student = NewStudent();

        student.ChangeCoachingStatus(CoachingStatus.Paused, "  Đi công tác  ", Actor, At);

        Assert.Equal(CoachingStatus.Paused, student.CoachingStatus);
        var row = Assert.Single(student.StatusHistory);
        Assert.Equal(CoachingStatus.Active, row.FromStatus);
        Assert.Equal(CoachingStatus.Paused, row.ToStatus);
        Assert.Equal(Actor, row.ActorUserId);
        Assert.Equal(At, row.At);
        Assert.Equal("Đi công tác", row.Reason);
    }

    // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-020: bị từ chối thì không thêm lịch sử
    [Fact]
    public void Same_status_is_rejected_without_history()
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.ChangeCoachingStatus(CoachingStatus.Active, "Nhầm", Actor, At));

        Assert.Equal("student.status_unchanged", ex.Code);
        Assert.Empty(student.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-018 @BR-f02-ho-so-hoc-vien-020
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_reason_is_rejected_and_status_unchanged(string? reason)
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.ChangeCoachingStatus(CoachingStatus.Paused, reason, Actor, At));

        Assert.Equal("student.reason_required", ex.Code);
        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Empty(student.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-018 @ADR-f02-ho-so-hoc-vien-009: lý do ≤ 500 (seed 501 loại khỏi living doc)
    [Fact]
    public void Reason_up_to_500_characters()
    {
        var student = NewStudent();
        student.ChangeCoachingStatus(CoachingStatus.Paused, new string('a', 500), Actor, At);

        var ex = Assert.Throws<DomainException>(() =>
            student.ChangeCoachingStatus(CoachingStatus.Ended, new string('a', 501), Actor, At));

        Assert.Equal("student.reason_too_long", ex.Code);
        Assert.Equal(CoachingStatus.Paused, student.CoachingStatus);
        Assert.Single(student.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-019: đổi được theo mọi chiều
    [Theory]
    [InlineData(CoachingStatus.Active, CoachingStatus.Paused)]
    [InlineData(CoachingStatus.Active, CoachingStatus.Ended)]
    [InlineData(CoachingStatus.Paused, CoachingStatus.Active)]
    [InlineData(CoachingStatus.Paused, CoachingStatus.Ended)]
    [InlineData(CoachingStatus.Ended, CoachingStatus.Active)]
    [InlineData(CoachingStatus.Ended, CoachingStatus.Paused)]
    public void Every_direction_is_allowed(CoachingStatus from, CoachingStatus to)
    {
        var student = NewStudent();
        if (from != CoachingStatus.Active)
            student.ChangeCoachingStatus(from, "Chuẩn bị", Actor, At);

        student.ChangeCoachingStatus(to, "Đổi", Actor, At.AddMinutes(1));

        Assert.Equal(to, student.CoachingStatus);
    }

    // @BR-f02-ho-so-hoc-vien-019: mở lại hồ sơ đã kết thúc, dữ liệu cũ giữ nguyên
    [Fact]
    public void Reopening_ended_student_keeps_data()
    {
        var student = NewStudent();
        student.ChangeCoachingStatus(CoachingStatus.Ended, "Hết gói", Actor, At);

        student.ChangeCoachingStatus(CoachingStatus.Active, "Mua gói mới", Actor, At.AddDays(1));

        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Equal("Giảm mỡ, giữ cơ", student.Registration.TrainingGoal);
        Assert.Equal(2, student.StatusHistory.Count);
    }

    // @BR-f02-ho-so-hoc-vien-022: tài khoản Chờ kích hoạt vẫn đổi được trạng thái coaching
    [Fact]
    public void Pending_account_can_change_coaching_status()
    {
        var student = NewStudent();
        Assert.Equal(AccountStatus.PendingActivation, student.AccountStatus);

        student.ChangeCoachingStatus(CoachingStatus.Paused, "Chưa chuyển khoản đủ", Actor, At);

        Assert.Equal(CoachingStatus.Paused, student.CoachingStatus);
    }

    // @BR-f02-ho-so-hoc-vien-021: lịch sử không sửa, không xóa được từ ngoài aggregate
    [Fact]
    public void History_is_read_only()
    {
        var student = NewStudent();
        student.ChangeCoachingStatus(CoachingStatus.Paused, "Đi công tác", Actor, At);

        var list = Assert.IsAssignableFrom<IList<CoachingStatusChange>>(student.StatusHistory);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
    }

    [Fact]
    public void Undefined_status_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => NewStudent().ChangeCoachingStatus((CoachingStatus)99, "x", Actor, At));
        Assert.Equal("student.status_invalid", ex.Code);
    }
}
