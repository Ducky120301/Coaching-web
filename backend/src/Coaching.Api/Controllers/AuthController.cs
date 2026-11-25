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
