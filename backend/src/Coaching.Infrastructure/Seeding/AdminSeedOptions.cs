namespace Coaching.Infrastructure.Seeding;

/// <summary>Tài khoản admin (coach) duy nhất của MVP. Mật khẩu lấy từ secret/env, KHÔNG commit.</summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string FullName { get; set; } = "Coach Duobexiuuu";
}
