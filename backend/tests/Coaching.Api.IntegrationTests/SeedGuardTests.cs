using Microsoft.AspNetCore.Mvc.Testing;

namespace Coaching.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SeedGuardTests(ApiFactory factory)
{
    // @BR-f00-codebase-020: học viên demo trùng email admin sẽ gán thêm role Student cho admin → từ chối khi khởi động
    [Fact]
    public void Demo_seed_email_equal_to_admin_email_stops_startup()
    {
        var ex = Assert.ThrowsAny<Exception>(() => factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("DemoSeed:Enabled", "true");
            b.UseSetting("DemoSeed:Email", ApiFactory.AdminEmail.ToUpperInvariant());
            b.UseSetting("DemoSeed:Password", "Demo@12345");
        }).CreateClient());

        Assert.Contains("DemoSeed:Email", ex.ToString());
    }
}
