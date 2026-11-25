using System.Text.Json.Serialization;

namespace Coaching.Application.Auth;

/// <summary>@ADR-f00-codebase-001: web đang đăng nhập; API kiểm vai trò trước khi cấp token.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ClientApp>))]
public enum ClientApp
{
    [JsonStringEnumMemberName("admin")] Admin,
    [JsonStringEnumMemberName("client")] Client,
}

public sealed record LoginRequest(string Email, string Password, ClientApp? App = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUser User);

public sealed record AuthUser(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles);

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-002: đăng nhập bằng mật khẩu tạm còn hạn — KHÔNG có phiên (không JWT),
/// chỉ có mã kích hoạt để đặt mật khẩu riêng. JSON: { activationRequired: true, activationToken, activationTokenExpiresAt }.
/// </summary>
public sealed record ActivationChallenge(bool ActivationRequired, string ActivationToken, DateTimeOffset ActivationTokenExpiresAt);

/// <summary>Kết quả đăng nhập thành công: đúng một trong hai có giá trị.</summary>
public sealed record LoginOutcome(AuthTokens? Tokens, ActivationChallenge? Challenge);
