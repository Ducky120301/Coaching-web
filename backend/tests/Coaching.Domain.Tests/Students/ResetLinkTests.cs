using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class ResetLinkTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At = new(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"),
        CreatedAt, RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));

    // @BR-f01-p1-tai-khoan-hoc-vien-017: Chờ kích hoạt → từ chối (dùng gửi thông tin đăng nhập), không thêm lịch sử
    [Fact]
    public void Reset_link_for_pending_account_is_rejected()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);

        var ex = Assert.Throws<DomainException>(() => student.RecordResetLinkSent(Coach, At.AddHours(1)));

        Assert.Equal("account.not_activated", ex.Code);
        Assert.Equal("Tài khoản chưa kích hoạt. Hãy gửi thông tin đăng nhập.", ex.Message);
        Assert.Single(student.AccountEvents);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-017: Đã kích hoạt → gửi được ở mọi trạng thái coaching
    [Theory]
    [InlineData(CoachingStatus.Active)]
    [InlineData(CoachingStatus.Paused)]
    [InlineData(CoachingStatus.Ended)]
    public void Reset_link_is_recorded_in_every_coaching_status(CoachingStatus status)
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);
        student.Activate(At.AddHours(1));
        if (status != CoachingStatus.Active)
            student.ChangeCoachingStatus(status, "Kịch bản", Coach, At.AddHours(2));

        student.RecordResetLinkSent(Coach, At.AddHours(3));

        var row = student.AccountEvents[^1];
        Assert.Equal(AccountEventType.ResetLinkSent, row.Type);
        Assert.Equal(At.AddHours(3), row.At);
        Assert.Equal(Coach, row.ActorUserId);
    }
}
