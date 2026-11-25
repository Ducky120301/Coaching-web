using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class ActivationTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SentAt = new(2026, 11, 1, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();

    private static Student SentStudent()
    {
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"), CreatedAt,
            RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));
        student.RecordCredentialsSent(Coach, SentAt);
        return student;
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: Đã kích hoạt kèm thời điểm, mật khẩu tạm hết hiệu lực ngay, thêm đúng một dòng lịch sử
    [Fact]
    public void Activate_moves_pending_to_activated_and_retires_temp_password()
    {
        var student = SentStudent();
        var at = SentAt.AddHours(5);

        student.Activate(at);

        Assert.Equal(AccountStatus.Activated, student.AccountStatus);
        Assert.Equal(at, student.ActivatedAt);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.False(student.IsTempPasswordValid(at));
        var row = student.AccountEvents[^1];
        Assert.Equal(AccountEventType.Activated, row.Type);
        Assert.Equal(at, row.At);
        Assert.Equal(student.UserId, row.ActorUserId);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: chỉ Chờ kích hoạt mới kích hoạt được
    [Fact]
    public void Activate_twice_is_rejected_without_history()
    {
        var student = SentStudent();
        student.Activate(SentAt.AddHours(1));
        var events = student.AccountEvents.Count;

        var ex = Assert.Throws<DomainException>(() => student.Activate(SentAt.AddHours(2)));

        Assert.Equal("account.already_activated", ex.Code);
        Assert.Equal(events, student.AccountEvents.Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-026: chưa gửi → đã gửi → hết hạn ở đúng 72 giờ → đã kích hoạt
    [Fact]
    public void Account_state_follows_send_expiry_and_activation()
    {
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"), CreatedAt,
            RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));
        Assert.Equal(AccountState.NotSent, student.GetAccountState(SentAt));

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.Equal(AccountState.Sent, student.GetAccountState(SentAt.AddHours(24)));
        Assert.Equal(AccountState.TempPasswordExpired, student.GetAccountState(SentAt.AddHours(72)));
        student.Activate(SentAt.AddHours(80));
        Assert.Equal(AccountState.Activated, student.GetAccountState(SentAt.AddHours(81)));
    }
}
