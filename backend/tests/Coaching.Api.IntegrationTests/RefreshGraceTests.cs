using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RefreshGraceTests(ApiFactory factory)
{
    private async Task<HttpResponseMessage> Refresh(string token) =>
        await factory.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(token));

    private async Task<AuthTokens> RefreshOk(string token)
    {
        var response = await Refresh(token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    // @ADR-f00-codebase-003
    [Fact]
    public async Task Repeated_grace_replays_keep_a_single_live_refresh_token()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var rt1 = await factory.LoginAsync(email, password);
        var rt2 = await RefreshOk(rt1.RefreshToken);
        _ = rt2;
        _ = await RefreshOk(rt1.RefreshToken);
        var replay2 = await RefreshOk(rt1.RefreshToken);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var live = await db.RefreshTokens.CountAsync(t => t.UserId == rt1.User.Id && t.RevokedAt == null);
            Assert.Equal(1, live);
        }

        // Mã cũ trong 30s vẫn được chấp nhận (grace) nhưng luôn chỉ còn một nhánh sống.
        await RefreshOk(replay2.RefreshToken);
    }

    // @BR-f00-codebase-013
    [Fact]
    public async Task Logout_replay_does_not_revoke_other_sessions()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var a = await factory.LoginAsync(email, password);
        var b = await factory.LoginAsync(email, password);

        var logout = await factory.CreateClient().PostAsJsonAsync("/api/auth/logout", new RefreshRequest(a.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(a.RefreshToken)).StatusCode);
        await RefreshOk(b.RefreshToken);
    }

    // @BR-f00-codebase-011
    [Fact]
    public async Task Grace_replay_does_not_revoke_other_sessions()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var a = await factory.LoginAsync(email, password);
        var b = await factory.LoginAsync(email, password);

        await RefreshOk(a.RefreshToken);
        await RefreshOk(a.RefreshToken);

        await RefreshOk(b.RefreshToken);
    }

    // @BR-f00-codebase-007 @BR-f00-codebase-010: khóa đăng nhập không làm chết phiên đang có
    [Fact]
    public async Task Refresh_still_works_while_account_is_locked_by_failed_logins()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);

        for (var i = 0; i < 5; i++)
            await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password", ClientApp.Client));
        var locked = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, ClientApp.Client));
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);

        await RefreshOk(tokens.RefreshToken);
    }
}
