using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Tests.Auth;

public sealed class PasswordErrorsTests
{
    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014
    [Theory]
    [InlineData(PasswordCheck.TooShort, "auth.password_too_short", "Mật khẩu cần ít nhất 8 ký tự.")]
    [InlineData(PasswordCheck.TooLong, "auth.password_too_long", "Mật khẩu tối đa 128 ký tự.")]
    [InlineData(PasswordCheck.Mismatch, "auth.password_mismatch", "Hai lần nhập mật khẩu không khớp.")]
    public void Password_check_maps_to_a_400_error(PasswordCheck check, string code, string message)
    {
        var error = AuthErrors.ForPassword(check);

        Assert.NotNull(error);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(ErrorKind.Validation, error.Kind);
    }

    [Fact]
    public void Valid_password_has_no_error() => Assert.Null(AuthErrors.ForPassword(PasswordCheck.Ok));

    // @BR-f01-p1-tai-khoan-hoc-vien-011: không có phiên hợp lệ để đặt mật khẩu → 401, hướng dẫn đăng nhập lại bằng mật khẩu tạm mới nhất
    [Fact]
    public void Activation_invalid_is_401_with_relogin_message()
    {
        Assert.Equal("auth.activation_invalid", AuthErrors.ActivationInvalid.Code);
        Assert.Equal("Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.", AuthErrors.ActivationInvalid.Message);
        Assert.Equal(ErrorKind.Unauthorized, AuthErrors.ActivationInvalid.Kind);
    }
}
