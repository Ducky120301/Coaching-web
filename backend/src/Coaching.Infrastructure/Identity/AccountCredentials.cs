using System.Security.Cryptography;
using Coaching.Application.Accounts;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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

    // @BR-f01-p1-tai-khoan-hoc-vien-015: lưu đúng từng ký tự (UserManager không trim); @BR-f01-p1-tai-khoan-hoc-vien-016: mật khẩu tạm bị thay
    public async Task SetOwnPasswordAsync(Guid userId, string password, CancellationToken ct = default) =>
        await ReplacePasswordAsync(await FindAsync(userId), password);

    // @BR-f01-p1-tai-khoan-hoc-vien-008: UserValidator so NormalizedEmail / NormalizedUserName → khác hoa/thường vẫn trùng; gồm cả coach
    public async Task<Result> ChangeEmailAsync(Guid userId, Email email, CancellationToken ct = default)
    {
        var user = await FindAsync(userId);
        user.Email = email.Value;
        user.UserName = email.Value; // tên đăng nhập = email (như StudentAccounts.CreatePendingAsync)

        IdentityResult updated;
        try
        {
            updated = await users.UpdateAsync(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result.Failure(StudentErrors.EmailTaken); // hai request đua nhau qua bước kiểm trùng → chỉ mục duy nhất chặn
        }

        if (updated.Succeeded)
            return Result.Success();
        if (updated.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            return Result.Failure(StudentErrors.EmailTaken);
        throw new InvalidOperationException(
            $"Không đổi được email đăng nhập: {string.Join("; ", updated.Errors.Select(e => e.Description))}");
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
