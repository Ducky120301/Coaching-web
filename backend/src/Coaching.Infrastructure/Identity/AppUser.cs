using Microsoft.AspNetCore.Identity;

namespace Coaching.Infrastructure.Identity;

/// <summary>Tài khoản đăng nhập (Admin hoặc Student). Dữ liệu nghiệp vụ nằm ở aggregate tương ứng.</summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public const int FullNameMaxLength = 100;

    public string FullName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
