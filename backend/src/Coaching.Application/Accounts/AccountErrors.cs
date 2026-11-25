using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

/// <summary>Lỗi của thao tác tài khoản học viên. Task 4/6/7 thêm các lỗi nghiệp vụ.</summary>
public static class AccountErrors
{
    // @BR-f01-p1-tai-khoan-hoc-vien-001
    public static readonly Error AlreadyActivated = new(
        "account.already_activated", "Tài khoản đã kích hoạt. Chỉ gửi được link đặt lại mật khẩu.", ErrorKind.Conflict);

    // @BR-f01-p1-tai-khoan-hoc-vien-002
    public static Error RequiredFieldsMissing(int count) =>
        new("account.required_fields_missing", $"Còn {count} mục bắt buộc chưa điền.", ErrorKind.Validation);

    // @BR-f01-p1-tai-khoan-hoc-vien-006 @ADR-f01-p1-tai-khoan-hoc-vien-004
    public static readonly Error EmailSendFailed =
        new("account.email_send_failed", "Chưa gửi được email. Vui lòng thử lại sau.", ErrorKind.Unavailable);

    // @BR-f01-p1-tai-khoan-hoc-vien-007
    public static readonly Error EmailLocked = new(
        "account.email_locked", "Tài khoản đã kích hoạt, không sửa được email.", ErrorKind.Conflict);

    // @NFR-f01-p1-tai-khoan-hoc-vien-003 @ADR-f01-p1-tai-khoan-hoc-vien-008
    public static readonly Error ClientUrlMissing =
        new("account.client_url_missing", "Chưa cấu hình địa chỉ web học viên nên chưa gửi được email.", ErrorKind.Unavailable);
}
