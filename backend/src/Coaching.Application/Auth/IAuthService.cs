using Coaching.Application.Common;

namespace Coaching.Application.Auth;

/// <summary>Đăng nhập / làm mới / thu hồi phiên. Cài đặt bằng ASP.NET Core Identity ở Infrastructure.</summary>
public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Đổi refresh token lấy cặp token mới. Refresh token cũ bị thu hồi (rotation).</summary>
    Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
