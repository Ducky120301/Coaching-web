using Coaching.Api.Common;
using Coaching.Application.Accounts.Commands;
using Coaching.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coaching.Api.Controllers;

/// <summary>Đăng nhập dùng chung cho Web Admin và Web Học viên.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.ToProblem(this);
        // @ADR-f01-p1-tai-khoan-hoc-vien-002: dạng cũ AuthTokens giữ nguyên; dạng mới { activationRequired, activationToken, activationTokenExpiresAt }
        return result.Value.Tokens is { } tokens ? Ok(tokens) : Ok(result.Value.Challenge);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: kích hoạt thành công → 200 AuthTokens (vào thẳng); không yêu cầu JWT, quyền đến từ mã kích hoạt
    [HttpPost("activate")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Activate(ActivateAccountCommand command, [FromServices] ActivateAccountHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);

    // @BR-f01-p1-tai-khoan-hoc-vien-023 @ADR-f01-p1-tai-khoan-hoc-vien-005: luôn 202 cùng một body; thư gửi nền.
    // Giới hạn theo IP là policy `auth`; giới hạn 60 giây theo tài khoản nằm trong use case (@BR-f01-p1-tai-khoan-hoc-vien-024).
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<ForgotPasswordResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, [FromServices] RequestPasswordResetHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status202Accepted, await handler.HandleAsync(command, cancellationToken));

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-021: không cần JWT, quyền đến từ token đã băm
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, [FromServices] ResetPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? NoContent() : result.Error!.ToProblem(this);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        (await auth.RefreshAsync(request, cancellationToken)).ToActionResult(this);

    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitPolicies.Logout)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }
}
