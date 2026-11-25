using System.Globalization;
using System.Net;
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Common;
using Coaching.Infrastructure.Time;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure.Emailing;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-008: nội dung 2 email tài khoản (bản nháp, coach duyệt trước khi bật Resend —
/// NFR-f01-p1-tai-khoan-hoc-vien-005). Địa chỉ lấy từ App:ClientBaseUrl.
/// </summary>
internal sealed class AccountEmails(IOptions<AppOptions> options) : IAccountEmails
{
    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";
    public const string PasswordResetSubject = "Đặt lại mật khẩu Duobexiuuu Coaching";
    private const string Signature = "Coach Duobexiuuu";

    public Result<EmailMessage> Credentials(string toEmail, string fullName, string temporaryPassword, DateTimeOffset expiresAt)
    {
        if (ClientBaseUrl() is not { } baseUrl)
            return AccountErrors.ClientUrlMissing; // @NFR-f01-p1-tai-khoan-hoc-vien-003

        var loginUrl = $"{baseUrl}/login";
        var expires = expiresAt.ToOffset(VietnamClock.Offset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
        const string lifetime = "Mật khẩu tạm có hiệu lực 72 giờ";
        const string firstLogin = "Lần đầu đăng nhập, bạn sẽ đặt mật khẩu riêng.";

        var text = string.Join('\n',
            $"Chào {fullName},",
            "",
            "Coach đã tạo tài khoản Duobexiuuu Coaching cho bạn.",
            "",
            $"Địa chỉ đăng nhập: {loginUrl}",
            $"Email: {toEmail}",
            $"Mật khẩu tạm: {temporaryPassword}",
            "",
            $"{lifetime} (đến {expires}). {firstLogin}",
            "",
            Signature);

        var html =
            $"<p>Chào {Encode(fullName)},</p>" +
            "<p>Coach đã tạo tài khoản Duobexiuuu Coaching cho bạn.</p>" +
            $"<p>Địa chỉ đăng nhập: <a href=\"{Encode(loginUrl)}\">{Encode(loginUrl)}</a><br>" +
            $"Email: {Encode(toEmail)}<br>" +
            $"Mật khẩu tạm: <strong>{Encode(temporaryPassword)}</strong></p>" +
            $"<p>{lifetime} (đến {expires}). {firstLogin}</p>" +
            $"<p>{Signature}</p>";

        return new EmailMessage(toEmail, CredentialsSubject, html, text);
    }

    public Result<EmailMessage> PasswordReset(string toEmail, string fullName, string rawToken)
    {
        if (ClientBaseUrl() is not { } baseUrl)
            return AccountErrors.ClientUrlMissing; // @NFR-f01-p1-tai-khoan-hoc-vien-003

        // Token nằm sau '#': không lên server, không lọt vào Referer/log
        var link = $"{baseUrl}/dat-lai-mat-khau#t={Uri.EscapeDataString(rawToken)}";
        const string notice = "Link có hiệu lực 30 phút và chỉ dùng được một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.";

        var text = string.Join('\n',
            $"Chào {fullName},",
            "",
            $"Bấm vào link sau để đặt mật khẩu mới cho tài khoản {toEmail}:",
            link,
            "",
            notice,
            "",
            Signature);

        var html =
            $"<p>Chào {Encode(fullName)},</p>" +
            $"<p>Bấm vào link sau để đặt mật khẩu mới cho tài khoản {Encode(toEmail)}:</p>" +
            $"<p><a href=\"{Encode(link)}\">{Encode(link)}</a></p>" +
            $"<p>{notice}</p>" +
            $"<p>{Signature}</p>";

        return new EmailMessage(toEmail, PasswordResetSubject, html, text);
    }

    /// <summary>Địa chỉ tuyệt đối http/https, bỏ dấu '/' cuối; trống hoặc hỏng → null (không gửi địa chỉ hỏng).</summary>
    private string? ClientBaseUrl()
    {
        var value = options.Value.ClientBaseUrl?.Trim().TrimEnd('/');
        return !string.IsNullOrEmpty(value)
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? value
                : null;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
