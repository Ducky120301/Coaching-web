using System.ComponentModel.DataAnnotations;

namespace Coaching.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "coaching-api";

    [Required]
    public string Audience { get; set; } = "coaching-web";

    /// <summary>Khóa ký HMAC-SHA256, tối thiểu 32 ký tự. Lấy từ secret/env, KHÔNG commit.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 14;
}
