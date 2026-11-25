using Coaching.Application.Common;

namespace Coaching.Application.Auth;

public static class AuthErrors
{
    // @BR-f00-codebase-006: một thông báo cho mọi lần đăng nhập thất bại (sai mk / email không tồn tại / đang khóa)
    public static readonly Error InvalidCredentials = new(
        "auth.invalid_credentials",
        "Email hoặc mật khẩu không đúng, hoặc bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút.",
        ErrorKind.Unauthorized);

    public static readonly Error InvalidRefreshToken =
        new("auth.invalid_refresh_token", "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", ErrorKind.Unauthorized);
}
