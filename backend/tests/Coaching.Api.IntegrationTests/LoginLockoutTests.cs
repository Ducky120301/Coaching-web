using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Auth;

namespace Coaching.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class LoginLockoutTests(ApiFactory factory)
{
    private async Task<HttpStatusCode> LoginAsync(string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, ClientApp.Client));
        return response.StatusCode;
    }

    // @BR-f00-codebase-007
    [Fact]
    public async Task Successful_login_resets_failed_attempts()
    {
        var (email, password) = await factory.CreateStudentAsync();

        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, "wrong-password"));
        Assert.Equal(HttpStatusCode.OK, await LoginAsync(email, password));

        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, "wrong-password"));
        Assert.Equal(HttpStatusCode.OK, await LoginAsync(email, password));
    }

    // @BR-f00-codebase-007
    [Fact]
    public async Task Concurrent_wrong_logins_are_all_counted()
    {
        var (email, password) = await factory.CreateStudentAsync();

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => LoginAsync(email, "wrong-password")));

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, password));
    }
}
