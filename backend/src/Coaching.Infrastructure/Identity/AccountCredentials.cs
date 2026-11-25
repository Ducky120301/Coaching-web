using System.Security.Cryptography;
using Coaching.Application.Accounts;
using Microsoft.AspNetCore.Identity;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu tạm / riêng là mật khẩu Identity — DB chỉ có PasswordHash.</summary>
internal sealed class AccountCredentials(UserManager<AppUser> users) : IAccountCredentials
{
    public const int TemporaryPasswordLength = 12;

    /// <summary>Bỏ ký tự dễ nhầm khi đọc từ email: 0/O/o, 1/l/I.</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    // @BR-f01-p1-tai-khoan-hoc-vien-003 @BR-f01-p1-tai-khoan-hoc-vien-005: mỗi lần gửi một mật khẩu tạm mới thay mật khẩu cũ
    public async Task<string> SetTemporaryPasswordAsync(Guid userId, CancellationToken ct = default)
    {
        var password = RandomNumberGenerator.GetString(Alphabet, TemporaryPasswordLength);
        await ReplacePasswordAsync(await FindAsync(userId), password);
        return password;
    }

    private async Task<AppUser> FindAsync(Guid userId) =>
        await users.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException($"Không tìm thấy tài khoản {userId}.");

    private async Task ReplacePasswordAsync(AppUser user, string password)
    {
        if (await users.HasPasswordAsync(user))
            EnsureSucceeded(await users.RemovePasswordAsync(user), "xóa mật khẩu cũ");
        EnsureSucceeded(await users.AddPasswordAsync(user, password), "đặt mật khẩu mới");
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Không thể {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
