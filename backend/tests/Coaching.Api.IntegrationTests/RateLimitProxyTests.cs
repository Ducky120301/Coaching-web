using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Microsoft.AspNetCore.TestHost;

namespace Coaching.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RateLimitProxyTests(ApiFactory factory)
{
    private HttpClient Client(int permit, int? forwardLimit = null) => factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("RateLimiting:AuthPermitPerMinute", permit.ToString());
        if (forwardLimit is { } limit)
            b.UseSetting("ForwardedHeaders:ForwardLimit", limit.ToString());
    }).CreateClient();

    private static async Task<HttpStatusCode> Login(HttpClient client, string xff)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest("nobody@test.local", "wrong-password", ClientApp.Client)),
        };
        request.Headers.Add("X-Forwarded-For", xff);
        return (await client.SendAsync(request)).StatusCode;
    }

    // @BR-f00-codebase-008 @ADR-f00-codebase-004: mặc định lấy hop cuối → client không chọn được bucket bằng entry đầu giả mạo
    [Fact]
    public async Task Spoofed_leading_xff_entry_cannot_pick_the_rate_limit_bucket()
    {
        var client = Client(permit: 3);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await Login(client, $"10.9.9.{i}, 203.0.113.50"));

        Assert.Equal(HttpStatusCode.TooManyRequests, await Login(client, "10.9.9.99, 203.0.113.50"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Login(client, "10.9.9.1, 203.0.113.51"));
    }

    // @ADR-f00-codebase-004: cấu hình ForwardedHeaders:ForwardLimit đổi được hop được tin (vd. Render thêm 1 proxy)
    [Fact]
    public async Task Forward_limit_is_configurable()
    {
        var client = Client(permit: 2, forwardLimit: 2);
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await Login(client, "198.51.100.7, 203.0.113.60"));

        Assert.Equal(HttpStatusCode.TooManyRequests, await Login(client, "198.51.100.7, 203.0.113.61"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Login(client, "198.51.100.8, 203.0.113.60"));
    }

    // @BR-f00-codebase-013: logout có bucket riêng, hết lượt đăng nhập vẫn đăng xuất được
    [Fact]
    public async Task Logout_is_not_blocked_by_exhausted_auth_bucket()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);
        var client = Client(permit: 2);
        for (var i = 0; i < 3; i++)
            await Login(client, "203.0.113.70");
        Assert.Equal(HttpStatusCode.TooManyRequests, await Login(client, "203.0.113.70"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new RefreshRequest(tokens.RefreshToken)),
        };
        request.Headers.Add("X-Forwarded-For", "203.0.113.70");
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(request)).StatusCode);
    }
}
