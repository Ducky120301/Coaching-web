namespace Coaching.Infrastructure.Emailing;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Log" (chỉ ghi log), "Resend" (gửi thật) hoặc "Capture" (dev/e2e: hộp thư trong bộ nhớ, cấm ở Production).</summary>
    public string Provider { get; set; } = "Log";

    /// <summary>Địa chỉ gửi, ví dụ "Duobexiuuu Coaching &lt;no-reply@your-domain.vn&gt;".</summary>
    public string From { get; set; } = "Duobexiuuu Coaching <no-reply@example.com>";

    public string? ResendApiKey { get; set; }
}
