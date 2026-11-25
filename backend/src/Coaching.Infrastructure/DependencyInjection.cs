using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Auth;
using Coaching.Application.Students;
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Persistence.Repositories;
using Coaching.Infrastructure.Audit;
using Coaching.Infrastructure.Emailing;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Coaching.Infrastructure.Seeding;
using Coaching.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8; // BR-07
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.Configure<DemoSeedOptions>(configuration.GetSection(DemoSeedOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, VietnamClock>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ICoachRepository, CoachRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentAccounts, StudentAccounts>();

        AddEmail(services, configuration, environment);

        return services;
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = EmailProviders.Resolve(configuration);
        if (EmailProviders.IsCapture(configuration))
        {
            // @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 1 — hộp thư đọc được qua API, không bao giờ bật ở Production
            if (environment.IsProduction())
                throw new InvalidOperationException(
                    "Email:Provider=Capture chỉ dùng cho dev/e2e, không được bật ở môi trường Production.");
            services.AddSingleton<EmailOutbox>();
            services.AddScoped<IEmailSender, CapturingEmailSender>();
        }
        else if (string.Equals(provider, EmailProviders.Resend, StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
                client.BaseAddress = new Uri("https://api.resend.com/"));
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        // Hàng đợi trong process cho email không cần chờ kết quả (Quên mật khẩu)
        services.AddSingleton<QueuedEmailSender>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<QueuedEmailSender>());
        services.AddHostedService<EmailQueueWorker>();

        // @ADR-f01-p1-tai-khoan-hoc-vien-008: thiếu App:ClientBaseUrl API vẫn chạy, chỉ lần gửi trả 503
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.AddSingleton<IAccountEmails, AccountEmails>();
    }

    public static Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        DatabaseInitializer.InitializeAsync(services, cancellationToken);
}
