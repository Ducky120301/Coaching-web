using Microsoft.Extensions.Configuration;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Giá trị hợp lệ của Email:Provider.</summary>
public static class EmailProviders
{
    public const string Log = "Log";
    public const string Resend = "Resend";

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-007: lưu thư trong bộ nhớ, đọc qua GET /api/test/emails. Cấm ở Production.</summary>
    public const string Capture = "Capture";

    public static string Resolve(IConfiguration configuration) =>
        configuration.GetSection(EmailOptions.SectionName)[nameof(EmailOptions.Provider)] is { Length: > 0 } provider
            ? provider
            : Log;

    public static bool IsCapture(IConfiguration configuration) =>
        string.Equals(Resolve(configuration), Capture, StringComparison.OrdinalIgnoreCase);
}
