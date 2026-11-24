using Coaching.Application.Common;

namespace Coaching.Application.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        new("auth.invalid_credentials", "Email hoặc mật khẩu không đúng.", ErrorKind.Unauthorized);

    public static readonly Error InvalidRefreshToken =
        new("auth.invalid_refresh_token", "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", ErrorKind.Unauthorized);

    public static readonly Error LockedOut =
        new("auth.locked_out", "Tài khoản tạm bị khóa do đăng nhập sai nhiều lần. Thử lại sau ít phút.", ErrorKind.Unauthorized);
}
