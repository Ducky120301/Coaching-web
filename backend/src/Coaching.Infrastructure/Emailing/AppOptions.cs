namespace Coaching.Infrastructure.Emailing;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-008: cấu hình "App" (Render: App__ClientBaseUrl).</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Địa chỉ public của web học viên, ví dụ "https://hv.duobexiuuu.vn". Trống → không gửi email tài khoản.</summary>
    public string? ClientBaseUrl { get; set; }
}
