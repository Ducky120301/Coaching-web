using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Auth;

namespace Coaching.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class LoginAppTests(ApiFactory factory)
{
    private async Task<HttpStatusCode> LoginAsync(string email, string password, ClientApp app)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, app));
        return response.StatusCode;
    }

    // @BR-f00-codebase-006 @ADR-f00-codebase-001
    [Fact]
    public async Task Wrong_password_and_wrong_app_returns_generic_401()
    {
        var (email, _) = await factory.CreateStudentAsync();
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password", ClientApp.Admin));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("auth.invalid_credentials", await response.Content.ReadAsStringAsync());
    }

    // @ADR-f00-codebase-001
    [Fact]
    public async Task Missing_app_returns_400()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("auth.app_required", await response.Content.ReadAsStringAsync());
    }

    // @BR-f00-codebase-005
    [Fact]
    public async Task Admin_logging_into_client_web_gets_403_without_session()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.AdminEmail, ApiFactory.AdminPassword, ClientApp.Client));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("auth.wrong_app", body);
        Assert.DoesNotContain("refreshToken", body);
    }

    // @BR-f00-codebase-007
    [Fact]
    public async Task Wrong_app_login_does_not_reset_failed_attempts()
    {
        var (email, password) = await factory.CreateStudentAsync();

        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, "wrong-password", ClientApp.Client));
        Assert.Equal(HttpStatusCode.Forbidden, await LoginAsync(email, password, ClientApp.Admin));
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, "wrong-password", ClientApp.Client));

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, password, ClientApp.Client));
    }
}
