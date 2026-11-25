using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Coaches;
using Coaching.Application.Common;

namespace Coaching.Api.IntegrationTests;

/// <summary>Tiêu chí nghiệm thu P0 (F00) + BR-15.</summary>
[Collection(ApiCollection.Name)]
public sealed class AuthAndAuthorizationTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_is_public()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_admin_can_login_and_read_admin_api()
    {
        var tokens = await factory.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        Assert.Contains(Roles.Admin, tokens.User.Roles);
        var response = await Send(HttpMethod.Get, "/api/admin/profile", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<CoachProfileDto>();
        Assert.Equal(ApiFactory.AdminEmail, profile!.Email);
    }

    [Fact]
    public async Task Admin_api_without_token_returns_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/admin/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Student_calling_admin_api_returns_403()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);

        var response = await Send(HttpMethod.Get, "/api/admin/profile", tokens.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_can_read_client_api()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);

        var response = await Send(HttpMethod.Get, "/api/client/me", tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_calling_client_api_returns_403()
    {
        var tokens = await factory.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var response = await Send(HttpMethod.Get, "/api/client/me", tokens.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_returns_401_with_error_code()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.AdminEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("auth.invalid_credentials", await response.Content.ReadAsStringAsync());
    }

    // @BR-f00-codebase-011
    // @ADR-f00-codebase-003
    [Fact]
    public async Task Refresh_rotates_token_and_immediate_replay_is_within_grace_period()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var first = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();

        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = (await refreshed.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        // Gửi lại ngay (≤ 30s) vẫn hợp lệ; phần quá 30s được kiểm ở acceptance với FakeTimeProvider.
        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return factory.CreateClient().SendAsync(request);
    }
}
