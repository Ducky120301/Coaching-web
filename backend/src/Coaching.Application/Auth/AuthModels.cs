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
