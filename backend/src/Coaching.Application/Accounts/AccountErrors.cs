using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

/// <summary>Lỗi của thao tác tài khoản học viên. Task 4/6/7 thêm các lỗi nghiệp vụ.</summary>
public static class AccountErrors
{
    public static readonly Error EmailSendFailed =
        new("account.email_send_failed", "Chưa gửi được email. Vui lòng thử lại sau.", ErrorKind.Unavailable);

    // @NFR-f01-p1-tai-khoan-hoc-vien-003 @ADR-f01-p1-tai-khoan-hoc-vien-008
    public static readonly Error ClientUrlMissing =
        new("account.client_url_missing", "Chưa cấu hình địa chỉ web học viên nên chưa gửi được email.", ErrorKind.Unavailable);
}
