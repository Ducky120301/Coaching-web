using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainEmail = Coaching.Domain.Common.Email;

namespace Coaching.Infrastructure.Seeding;

/// <summary>Chạy migration, tạo role và tài khoản admin (idempotent).</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = sp.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)), $"tạo role {role}");
        }

        var seed = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrEmpty(seed.Password))
        {
            logger.LogWarning("Bỏ qua seed admin: chưa cấu hình AdminSeed:Email / AdminSeed:Password.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var clock = sp.GetRequiredService<IClock>();
        var email = DomainEmail.Create(seed.Email);

        var admin = await userManager.FindByEmailAsync(email.Value);
        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = email.Value,
                Email = email.Value,
                EmailConfirmed = true,
                FullName = seed.FullName,
                CreatedAt = clock.UtcNow,
            };
            EnsureSucceeded(await userManager.CreateAsync(admin, seed.Password), "tạo tài khoản admin");
            logger.LogInformation("Đã tạo tài khoản admin {Email}.", email.Value);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin), "gán role Admin");

        if (!await db.Coaches.AnyAsync(c => c.UserId == admin.Id, cancellationToken))
        {
            db.Coaches.Add(Coach.Create(admin.Id, seed.FullName, email));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Không thể {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
