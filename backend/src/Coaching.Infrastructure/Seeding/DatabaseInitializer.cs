using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainEmail = Coaching.Domain.Common.Email;

namespace Coaching.Infrastructure.Seeding;

/// <summary>Kiểm schema (do Liquibase tạo), tạo role, admin + hồ sơ coach và (tuỳ môi trường) học viên demo. Idempotent.</summary>
public static class DatabaseInitializer
{
    public const int MinPasswordLength = 8;

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = sp.GetRequiredService<AppDbContext>();

        var adminSeed = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        var demoSeed = sp.GetRequiredService<IOptions<DemoSeedOptions>>().Value;
        ValidateSeedCredentials("AdminSeed", adminSeed.Email, adminSeed.Password);
        // Trùng email sẽ gán thêm role Student cho admin → admin vào được web học viên (phá BR-004/005).
        if (demoSeed.Enabled && string.Equals(demoSeed.Email?.Trim(), adminSeed.Email!.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "DemoSeed:Email không được trùng AdminSeed:Email — hệ thống không khởi động.");

        // @ADR-f00-codebase-011: schema do Liquibase quản lý; API không migrate, chỉ kiểm schema đã có rồi seed
        await EnsureSchemaExistsAsync(db, cancellationToken);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)), $"tạo role {role}");
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var clock = sp.GetRequiredService<IClock>();

        // @BR-f00-codebase-016 @BR-f00-codebase-019: chỉ tạo khi chưa có; không đổi mật khẩu admin đã tồn tại
        var adminEmail = DomainEmail.Create(adminSeed.Email);
        var admin = await EnsureUserAsync(userManager, clock, adminEmail.Value, adminSeed.Password!, adminSeed.FullName, Roles.Admin, logger);
        if (!await db.Coaches.AnyAsync(c => c.UserId == admin.Id, cancellationToken))
        {
            db.Coaches.Add(Coach.Create(admin.Id, adminSeed.FullName, adminEmail));
            await db.SaveChangesAsync(cancellationToken);
        }

        // @BR-f00-codebase-020
        if (demoSeed.Enabled)
        {
            ValidateSeedCredentials("DemoSeed", demoSeed.Email, demoSeed.Password);
            var demoEmail = DomainEmail.Create(demoSeed.Email);
            var demo = await EnsureUserAsync(userManager, clock, demoEmail.Value, demoSeed.Password!, demoSeed.FullName, Roles.Student, logger);

            // @ADR-f02-ho-so-hoc-vien-001: học viên demo (seed từ F00) cần hồ sơ Student — bổ sung nếu thiếu, idempotent
            if (!await db.Students.AnyAsync(s => s.UserId == demo.Id, cancellationToken))
            {
                var coach = await db.Coaches.SingleAsync(c => c.UserId == admin.Id, cancellationToken);
                db.Students.Add(Student.Create(coach.Id, demo.Id, demoSeed.FullName, demoEmail, clock.UtcNow));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static async Task EnsureSchemaExistsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var applied = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = 'DATABASECHANGELOG'")
            .SingleAsync(cancellationToken);
        if (applied == 0)
            throw new InvalidOperationException(
                "Database chưa có schema. Chạy Liquibase (`liquibase update`, xem database/) trước khi khởi động API.");
    }

    // @BR-f00-codebase-017 @BR-f00-codebase-018: thiếu cấu hình hoặc mật khẩu < 8 ký tự → không khởi động
    private static void ValidateSeedCredentials(string section, string? email, string? password)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException($"Thiếu cấu hình {section}:Email — hệ thống không khởi động.");
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException($"Thiếu cấu hình {section}:Password — hệ thống không khởi động.");
        if (password.Length < MinPasswordLength)
            throw new InvalidOperationException(
                $"{section}:Password phải có ít nhất {MinPasswordLength} ký tự — hệ thống không khởi động.");
    }

    private static async Task<AppUser> EnsureUserAsync(UserManager<AppUser> userManager, IClock clock,
        string email, string password, string fullName, string role, ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                CreatedAt = clock.UtcNow,
            };
            EnsureSucceeded(await userManager.CreateAsync(user, password), $"tạo tài khoản {email}");
            logger.LogInformation("Đã tạo tài khoản {Email} (role {Role}).", email, role);
        }

        if (!await userManager.IsInRoleAsync(user, role))
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"gán role {role} cho {email}");
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Không thể {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
