using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f02-ho-so-hoc-vien-001: tạo tài khoản Identity "Chờ kích hoạt" (không mật khẩu, role Student).</summary>
internal sealed class StudentAccounts(UserManager<AppUser> users, IClock clock) : IStudentAccounts
{
    public async Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            Id = userId,
            UserName = email.Value,
            Email = email.Value,
            FullName = fullName,
            EmailConfirmed = false,
            CreatedAt = clock.UtcNow,
        };

        IdentityResult created;
        try
        {
            created = await users.CreateAsync(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // @BR-f02-ho-so-hoc-vien-001: hai request cùng email đua nhau qua bước kiểm trùng của Identity → chỉ mục duy nhất chặn, trả 409 thay vì 500
            return Result.Failure(StudentErrors.EmailTaken);
        }
        if (!created.Succeeded)
        {
            // @BR-f02-ho-so-hoc-vien-001: Identity so NormalizedEmail → khác hoa/thường vẫn là trùng; gồm cả tài khoản coach
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                return Result.Failure(StudentErrors.EmailTaken);
            throw new InvalidOperationException(
                $"Không tạo được tài khoản học viên: {string.Join("; ", created.Errors.Select(e => e.Description))}");
        }

        var role = await users.AddToRoleAsync(user, Roles.Student);
        if (!role.Succeeded)
            throw new InvalidOperationException(
                $"Không gán được role Student: {string.Join("; ", role.Errors.Select(e => e.Description))}");
        return Result.Success();
    }
}
