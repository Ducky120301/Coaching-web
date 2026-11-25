using Coaching.Application.Common;
using Coaching.Domain.Students;

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

    public static readonly Error AppRequired =
        new("auth.app_required", "Thiếu thông tin web đăng nhập.", ErrorKind.Validation);

    // @BR-f00-codebase-004 @BR-f00-codebase-005
    public static Error WrongApp(ClientApp app) => new(
        "auth.wrong_app",
        app == ClientApp.Admin
            ? "Tài khoản này không có quyền vào khu vực coach."
            : "Tài khoản này không có quyền vào khu vực học viên.",
        ErrorKind.Forbidden);

    // @BR-f01-p1-tai-khoan-hoc-vien-011 @BR-f01-p1-tai-khoan-hoc-vien-016: mã kích hoạt hết hạn / đã bị hủy (coach gửi lại, sửa email) / đã dùng
    public static readonly Error ActivationInvalid = new(
        "auth.activation_invalid",
        "Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.",
        ErrorKind.Unauthorized);

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    public static readonly Error PasswordTooShort = new("auth.password_too_short", "Mật khẩu cần ít nhất 8 ký tự.");

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    public static readonly Error PasswordTooLong = new("auth.password_too_long", "Mật khẩu tối đa 128 ký tự.");

    // @BR-f01-p1-tai-khoan-hoc-vien-014
    public static readonly Error PasswordMismatch = new("auth.password_mismatch", "Hai lần nhập mật khẩu không khớp.");

    /// <summary>Lỗi 400 tương ứng kết quả PasswordPolicy.Check; null khi mật khẩu hợp lệ. Dùng cho kích hoạt và đặt lại.</summary>
    public static Error? ForPassword(PasswordCheck check) => check switch
    {
        PasswordCheck.TooShort => PasswordTooShort,
        PasswordCheck.TooLong => PasswordTooLong,
        PasswordCheck.Mismatch => PasswordMismatch,
        _ => null,
    };

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-020: hết hạn / đã dùng / đã có link mới → cùng một lỗi
    public static readonly Error ResetLinkInvalid = new(
        "auth.reset_link_invalid", "Link đặt lại đã hết hạn hoặc đã được dùng. Hãy xin link mới.");
}
