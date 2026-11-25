using Coaching.Application.Students;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Tests.Students;

public sealed class StudentDetailDtoTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);

    // @ADR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ trả kèm trạng thái + lịch sử tài khoản (rỗng khi mới tạo)
    [Fact]
    public void Detail_contains_account_info_of_new_student()
    {
        var coach = Coach.Create(Guid.NewGuid(), "Coach Duobexiuuu", Email.Create("coach@duobexiuuu.vn"));
        var student = Student.Create(coach.Id, Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"), Now);

        var dto = StudentDetailDto.From(student, coach, Now);

        Assert.Equal("NotSent", dto.Account.State);
        Assert.Null(dto.Account.CredentialsSentAt);
        Assert.Null(dto.Account.TempPasswordExpiresAt);
        Assert.Null(dto.Account.ActivatedAt);
        Assert.Empty(dto.Account.History);
    }
}
