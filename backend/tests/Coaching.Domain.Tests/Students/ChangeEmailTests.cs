using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class ChangeEmailTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At = new(2026, 11, 2, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Email Typo = Email.Create("quan@gmial.com");
    private static readonly Email Fixed = Email.Create("quan@gmail.com");

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Typo, CreatedAt,
        RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));

    // @BR-f01-p1-tai-khoan-hoc-vien-010: chưa gửi lần nào → chỉ đổi email (và khóa tìm kiếm), không cần gửi thư
    [Fact]
    public void Change_before_any_send_only_changes_the_email()
    {
        var student = NewStudent();

        var mustSend = student.ChangeEmail(Fixed, Coach, At);

        Assert.False(mustSend);
        Assert.Equal(Fixed, student.Email);
        Assert.Contains("quan@gmail.com", student.SearchKey);
        Assert.Null(student.CredentialsSentAt);
        var row = Assert.Single(student.AccountEvents);
        Assert.Equal(AccountEventType.EmailChanged, row.Type);
        Assert.Equal("quan@gmial.com", row.FromEmail);
        Assert.Equal("quan@gmail.com", row.ToEmail);
        Assert.Equal(Coach, row.ActorUserId);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009: đã gửi → phải gửi thông tin mới tới email mới; hạn 72 giờ tính lại từ lúc sửa
    [Fact]
    public void Change_after_send_asks_for_new_credentials_with_fresh_expiry()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);
        var changedAt = At.AddHours(1);

        var mustSend = student.ChangeEmail(Fixed, Coach, changedAt);

        Assert.True(mustSend);
        Assert.Equal(changedAt, student.CredentialsSentAt);
        Assert.Equal(changedAt.AddHours(72), student.TempPasswordExpiresAt);
        Assert.Equal(new[] { AccountEventType.CredentialsSent, AccountEventType.EmailChanged },
            student.AccountEvents.Select(e => e.Type).ToArray());
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-007: đã kích hoạt → không sửa được email, không thêm lịch sử
    [Fact]
    public void Change_on_activated_account_is_rejected()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);
        student.Activate(At.AddHours(1));
        var events = student.AccountEvents.Count;

        var ex = Assert.Throws<DomainException>(() => student.ChangeEmail(Fixed, Coach, At.AddHours(2)));

        Assert.Equal("account.email_locked", ex.Code);
        Assert.Equal(Typo, student.Email);
        Assert.Equal(events, student.AccountEvents.Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-027: cùng email (sau chuẩn hóa) không phải là một lần sửa → không thêm dòng
    [Fact]
    public void Same_email_changes_nothing()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);

        var mustSend = student.ChangeEmail(Email.Create(" QUAN@gmial.com "), Coach, At.AddHours(1));

        Assert.False(mustSend);
        Assert.Equal(At, student.CredentialsSentAt);
        Assert.Single(student.AccountEvents);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-027: mỗi việc thêm đúng một dòng theo thứ tự; việc bị từ chối không thêm dòng
    [Fact]
    public void Account_history_gains_exactly_one_line_per_action_in_order()
    {
        var student = NewStudent();

        student.RecordCredentialsSent(Coach, At);
        student.RecordCredentialsSent(Coach, At.AddHours(1));
        student.ChangeEmail(Fixed, Coach, At.AddHours(2));
        student.Activate(At.AddHours(3));
        Assert.Throws<DomainException>(() => student.RecordCredentialsSent(Coach, At.AddHours(4)));

        Assert.Equal(
            new[] { AccountEventType.CredentialsSent, AccountEventType.CredentialsResent, AccountEventType.EmailChanged, AccountEventType.Activated },
            student.AccountEvents.Select(e => e.Type).ToArray());
        Assert.Equal(new[] { At, At.AddHours(1), At.AddHours(2), At.AddHours(3) }, student.AccountEvents.Select(e => e.At).ToArray());
    }
}
