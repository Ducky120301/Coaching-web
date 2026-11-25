using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class PasswordPolicyTests
{
    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-015: 8–128 ký tự, giữ nguyên dấu và khoảng trắng
    [Theory]
    [InlineData("abcdefgh")]
    [InlineData("Coach2026")]
    [InlineData(" Coach2026 ")]
    [InlineData("Mậtkhẩu1")]
    public void Accepts_8_to_128_characters_as_typed(string password) =>
        Assert.Equal(PasswordCheck.Ok, PasswordPolicy.Check(password, password));

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    [Fact]
    public void Accepts_exactly_128_characters()
    {
        var password = new string('a', 128);
        Assert.Equal(PasswordCheck.Ok, PasswordPolicy.Check(password, password));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("abcdefg")]
    public void Rejects_shorter_than_8(string? password) =>
        Assert.Equal(PasswordCheck.TooShort, PasswordPolicy.Check(password, password));

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    [Fact]
    public void Rejects_129_characters()
    {
        var password = new string('a', 129);
        Assert.Equal(PasswordCheck.TooLong, PasswordPolicy.Check(password, password));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-014 @BR-f01-p1-tai-khoan-hoc-vien-015: so khớp từng ký tự (ordinal), không trim
    [Theory]
    [InlineData("Coach2026", "Coach2027")]
    [InlineData(" Coach2026 ", "Coach2026")]
    [InlineData("Coach2026", "coach2026")]
    [InlineData("Coach2026", null)]
    public void Rejects_confirmation_that_differs_by_any_character(string password, string? confirm) =>
        Assert.Equal(PasswordCheck.Mismatch, PasswordPolicy.Check(password, confirm));
}
