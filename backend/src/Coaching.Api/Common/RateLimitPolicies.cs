using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Coaching.Api.Common;

internal static class RateLimitPolicies
{
    public const string Auth = "auth";

    /// <summary>Giới hạn số lần gọi endpoint đăng nhập/làm mới token theo IP (mặc định 10 lần/phút).</summary>
    public static IServiceCollection AddCoachingRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitPerMinute = configuration.GetValue("RateLimiting:AuthPermitPerMinute", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitPerMinute, Window = TimeSpan.FromMinutes(1) }));
        });
        return services;
    }
}
