using System.IdentityModel.Tokens.Jwt;
using Coaching.Application.Abstractions;

namespace Coaching.Api.Auth;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public string? Email => Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public string? FullName => Principal?.FindFirst(JwtRegisteredClaimNames.Name)?.Value;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
