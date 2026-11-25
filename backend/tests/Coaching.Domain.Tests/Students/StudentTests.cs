using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class StudentTests
{
    private static readonly Guid CoachId = Guid.NewGuid();
    private static readonly DateTimeOffset At = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);
    private static readonly Email LanEmail = Email.Create("lan@gmail.com");

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007
    [Fact]
    public void Create_sets_owner_and_default_statuses()
    {
        var userId = Guid.NewGuid();

        var student = Student.Create(CoachId, userId, "  Lan  ", LanEmail, At);

        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal(CoachId, student.CoachId);
        Assert.Equal(userId, student.UserId);
        Assert.Equal("Lan", student.FullName);
        Assert.Equal(LanEmail, student.Email);
        Assert.Equal(AccountStatus.PendingActivation, student.AccountStatus);
        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Equal(At, student.CreatedAt);
    }

    // @BR-f02-ho-so-hoc-vien-003
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_full_name(string? fullName)
    {
        var ex = Assert.Throws<DomainException>(() => Student.Create(CoachId, Guid.NewGuid(), fullName, LanEmail, At));
        Assert.Equal("student.full_name_required", ex.Code);
    }

    // @BR-f02-ho-so-hoc-vien-003 @ADR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Create_accepts_100_characters_and_rejects_101()
    {
        Assert.Equal(100, Student.Create(CoachId, Guid.NewGuid(), new string('a', 100), LanEmail, At).FullName.Length);

        var ex = Assert.Throws<DomainException>(() =>
            Student.Create(CoachId, Guid.NewGuid(), new string('a', 101), LanEmail, At));
        Assert.Equal("student.full_name_too_long", ex.Code);
    }

    [Fact]
    public void Create_requires_coach_and_login_account()
    {
        Assert.Equal("student.coach_required",
            Assert.Throws<DomainException>(() => Student.Create(Guid.Empty, Guid.NewGuid(), "Lan", LanEmail, At)).Code);
        Assert.Equal("student.user_required",
            Assert.Throws<DomainException>(() => Student.Create(CoachId, Guid.Empty, "Lan", LanEmail, At)).Code);
    }
}
