using Coaching.Application.Common;

namespace Coaching.Application.Auth;

/// <summary>Đăng nhập / làm mới / thu hồi phiên. Cài đặt bằng ASP.NET Core Identity ở Infrastructure.</summary>
public interface IAuthService
{
    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: học viên Chờ kích hoạt + mật khẩu tạm còn hạn → Challenge thay vì Tokens.</summary>
    Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Đổi refresh token lấy cặp token mới. Refresh token cũ bị thu hồi (rotation).</summary>
    Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
