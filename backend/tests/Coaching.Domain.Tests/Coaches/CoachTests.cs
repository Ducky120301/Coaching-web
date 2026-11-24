using Coaching.Domain.Coaches;
using Coaching.Domain.Common;

namespace Coaching.Domain.Tests.Coaches;

public sealed class CoachTests
{
    private static readonly Email AnyEmail = Email.Create("coach@duobexiuuu.vn");

    [Fact]
    public void Create_sets_trimmed_name_and_new_id()
    {
        var userId = Guid.NewGuid();

        var coach = Coach.Create(userId, "  Coach Duobexiuuu ", AnyEmail);

        Assert.NotEqual(Guid.Empty, coach.Id);
        Assert.Equal(userId, coach.UserId);
        Assert.Equal("Coach Duobexiuuu", coach.DisplayName);
        Assert.Equal(AnyEmail, coach.Email);
    }

    [Fact]
    public void Create_requires_login_account()
    {
        var ex = Assert.Throws<DomainException>(() => Coach.Create(Guid.Empty, "Coach", AnyEmail));
        Assert.Equal("coach.user_required", ex.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_name(string name)
    {
        var ex = Assert.Throws<DomainException>(() => Coach.Create(Guid.NewGuid(), name, AnyEmail));
        Assert.Equal("coach.name_required", ex.Code);
    }

    [Fact]
    public void Rename_rejects_name_longer_than_limit()
    {
        var coach = Coach.Create(Guid.NewGuid(), "Coach", AnyEmail);

        var ex = Assert.Throws<DomainException>(() => coach.Rename(new string('a', Coach.DisplayNameMaxLength + 1)));

        Assert.Equal("coach.name_too_long", ex.Code);
        Assert.Equal("Coach", coach.DisplayName);
    }
}
