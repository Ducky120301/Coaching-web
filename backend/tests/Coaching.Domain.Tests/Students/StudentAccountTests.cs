using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class StudentAccountTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);

    private static Student NewStudent() =>
        Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"), CreatedAt);

    // @ADR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ mới chưa có dòng lịch sử tài khoản nào
    [Fact]
    public void New_student_has_not_sent_state_and_empty_account_history()
    {
        var student = NewStudent();

        Assert.Equal(AccountState.NotSent, student.GetAccountState(CreatedAt.AddDays(10)));
        Assert.Null(student.CredentialsSentAt);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.Null(student.ActivatedAt);
        Assert.Empty(student.AccountEvents);
    }

    [Fact]
    public void Temp_password_lifetime_is_72_hours()
    {
        Assert.Equal(TimeSpan.FromHours(72), Student.TempPasswordLifetime);
    }
}
