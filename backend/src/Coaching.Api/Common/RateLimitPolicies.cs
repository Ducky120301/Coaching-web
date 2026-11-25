using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coaching.Api.Common;

internal static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Logout = "logout";

    /// <summary>@BR-f00-codebase-008: mỗi IP thật tối đa N yêu cầu đăng nhập/gia hạn mỗi phút (mặc định 10).</summary>
    public static IServiceCollection AddCoachingRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitPerMinute = configuration.GetValue("RateLimiting:AuthPermitPerMinute", 10);
        var logoutPermitPerMinute = configuration.GetValue("RateLimiting:LogoutPermitPerMinute", 60);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitPerMinute, Window = TimeSpan.FromMinutes(1) }));
            // Đăng xuất có bucket riêng (không bị hết lượt đăng nhập chặn) để mã làm mới luôn thu hồi được.
            options.AddPolicy(Logout, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                "logout:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = logoutPermitPerMinute, Window = TimeSpan.FromMinutes(1) }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                    : 60;
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = $"Bạn thao tác quá nhanh. Vui lòng thử lại sau {seconds} giây.",
                    Type = "rate_limited",
                };
                problem.Extensions["code"] = "rate_limited";
                await response.WriteAsJsonAsync(problem, cancellationToken);
            };
        });
        return services;
    }
}
