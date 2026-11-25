using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class SendCredentialsTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SentAt = new(2026, 11, 25, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();

    private static Student CompleteStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân",
        Email.Create("quan@gmail.com"), CreatedAt, RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 20)));

    // @BR-f01-p1-tai-khoan-hoc-vien-002: lần gửi đầu cần đủ mục bắt buộc; bị từ chối thì không đổi gì
    [Fact]
    public void First_send_needs_every_required_field()
    {
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"), CreatedAt);
        var missing = student.MissingRequiredFields.Count;

        var ex = Assert.Throws<DomainException>(() => student.RecordCredentialsSent(Coach, SentAt));

        Assert.Equal("account.required_fields_missing", ex.Code);
        Assert.Equal($"Còn {missing} mục bắt buộc chưa điền.", ex.Message);
        Assert.Null(student.CredentialsSentAt);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.Empty(student.AccountEvents);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-004: hạn mật khẩu tạm = lúc gửi + 72 giờ
    [Fact]
    public void First_send_sets_72_hour_expiry_and_records_CredentialsSent()
    {
        var student = CompleteStudent();

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.Equal(SentAt, student.CredentialsSentAt);
        Assert.Equal(SentAt.AddHours(72), student.TempPasswordExpiresAt);
        var row = Assert.Single(student.AccountEvents);
        Assert.Equal(AccountEventType.CredentialsSent, row.Type);
        Assert.Equal(SentAt, row.At);
        Assert.Equal(Coach, row.ActorUserId);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-005: gửi lại không kiểm mục bắt buộc, hạn tính lại từ lần gửi sau cùng
    [Fact]
    public void Resend_skips_required_fields_and_records_CredentialsResent()
    {
        var student = CompleteStudent();
        student.RecordCredentialsSent(Coach, SentAt);
        student.UpdateRegistration("Quân", RegistrationForm.CreateEmpty(), Coach, SentAt.AddHours(1));
        var resentAt = SentAt.AddHours(2);

        student.RecordCredentialsSent(Coach, resentAt);

        Assert.Equal(resentAt, student.CredentialsSentAt);
        Assert.Equal(resentAt.AddHours(72), student.TempPasswordExpiresAt);
        Assert.Equal(new[] { AccountEventType.CredentialsSent, AccountEventType.CredentialsResent },
            student.AccountEvents.Select(e => e.Type).ToArray());
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001: tài khoản đã kích hoạt → từ chối, không thêm lịch sử
    [Fact]
    public void Send_to_activated_account_is_rejected()
    {
        var student = CompleteStudent();
        student.Activate(SentAt);
        var events = student.AccountEvents.Count;

        var ex = Assert.Throws<DomainException>(() => student.RecordCredentialsSent(Coach, SentAt.AddHours(1)));

        Assert.Equal("account.already_activated", ex.Code);
        Assert.Equal(events, student.AccountEvents.Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001: gửi được ở mọi trạng thái coaching
    [Theory]
    [InlineData(CoachingStatus.Paused)]
    [InlineData(CoachingStatus.Ended)]
    public void Send_works_in_every_coaching_status(CoachingStatus status)
    {
        var student = CompleteStudent();
        student.ChangeCoachingStatus(status, "Kịch bản", Coach, SentAt);

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.Equal(SentAt, student.CredentialsSentAt);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-004: 71 giờ 59 phút còn dùng được; đúng 72 giờ và 73 giờ thì hết
    [Fact]
    public void Temp_password_expires_exactly_at_72_hours()
    {
        var student = CompleteStudent();
        Assert.False(student.IsTempPasswordValid(SentAt));

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.True(student.IsTempPasswordValid(SentAt.AddHours(48)));
        Assert.True(student.IsTempPasswordValid(SentAt.AddHours(71).AddMinutes(59)));
        Assert.False(student.IsTempPasswordValid(SentAt.AddHours(72)));
        Assert.False(student.IsTempPasswordValid(SentAt.AddHours(73)));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-012: sau khi kích hoạt, mật khẩu tạm không còn hiệu lực
    [Fact]
    public void Activated_account_has_no_valid_temp_password()
    {
        var student = CompleteStudent();
        student.RecordCredentialsSent(Coach, SentAt);

        student.Activate(SentAt.AddHours(1));

        Assert.Equal(AccountStatus.Activated, student.AccountStatus);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.False(student.IsTempPasswordValid(SentAt.AddHours(2)));
    }
}
