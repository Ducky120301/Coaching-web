using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Acceptance.Support;

/// <summary>API thật, cấu hình + đồng hồ do scenario quyết định.</summary>
public sealed class ApiHost(string connectionString, IReadOnlyDictionary<string, string?> settings, TimeProvider clock)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(clock);
        });
    }
}
