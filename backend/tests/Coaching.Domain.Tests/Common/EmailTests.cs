using Coaching.Domain.Common;

namespace Coaching.Domain.Tests.Common;

public sealed class EmailTests
{
    [Theory]
    [InlineData("Duc.Hoang@Gmail.com", "duc.hoang@gmail.com")]
    [InlineData("  quan.pham@gmail.com  ", "quan.pham@gmail.com")]
    public void Create_normalizes_to_trimmed_lowercase(string raw, string expected) =>
        Assert.Equal(expected, Email.Create(raw).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_missing_email(string? raw)
    {
        var ex = Assert.Throws<DomainException>(() => Email.Create(raw));
        Assert.Equal("email.required", ex.Code);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("Ten <a@b.com>")]
    [InlineData("a b@c.com")]
    public void Create_rejects_invalid_email(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => Email.Create(raw));
        Assert.Equal("email.invalid", ex.Code);
    }

    [Fact]
    public void Emails_with_same_normalized_value_are_equal() =>
        Assert.Equal(Email.Create("A@X.com"), Email.Create("a@x.com"));
}
