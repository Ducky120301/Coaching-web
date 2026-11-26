using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportMidFileFailureTests(ApiFactory factory)
{
    // @BR-f02-import-excel-033: quá trình dừng ở dòng 4 (dòng dữ liệu thứ 3) → hồ sơ dòng 2, 3 đã tạo vẫn còn;
    // dòng lỗi và các dòng sau không có hồ sơ (mỗi dòng một transaction)
    [Fact]
    public async Task Profiles_created_before_a_crash_are_kept()
    {
        using var crashing = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<IStudentAccounts>();
            services.AddScoped<StudentAccounts>();
            services.AddScoped<IStudentAccounts>(sp => new CrashOnEmail(sp.GetRequiredService<StudentAccounts>()));
        }));
        var admin = crashing.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminEmail, ApiFactory.AdminPassword, ClientApp.Admin));
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var first = ImportRequests.NewEmail();
        var second = ImportRequests.NewEmail();
        var crash = $"{CrashOnEmail.Prefix}{Guid.NewGuid():N}@test.local";
        var after = ImportRequests.NewEmail();
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(first, "Dòng 2");
        workbook.AddValidRow(second, "Dòng 3");
        workbook.AddValidRow(crash, "Dòng 4 — dừng ở đây");
        workbook.AddValidRow(after, "Dòng 5");

        var response = await ImportRequests.PostAsync(admin, workbook.ToBytes());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(await ImportRequests.StudentExistsAsync(factory, first));
        Assert.True(await ImportRequests.StudentExistsAsync(factory, second));
        Assert.False(await ImportRequests.StudentExistsAsync(factory, crash));
        Assert.False(await ImportRequests.StudentExistsAsync(factory, after));
    }

    /// <summary>Giả lập mất kết nối giữa chừng: tạo tài khoản ném lỗi với email bắt đầu bằng <see cref="Prefix"/>.</summary>
    private sealed class CrashOnEmail(IStudentAccounts inner) : IStudentAccounts
    {
        public const string Prefix = "crash-";

        public Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName,
            CancellationToken cancellationToken = default) =>
            email.Value.StartsWith(Prefix, StringComparison.Ordinal)
                ? throw new InvalidOperationException("Mất kết nối cơ sở dữ liệu giữa chừng (giả lập).")
                : inner.CreatePendingAsync(userId, email, fullName, cancellationToken);
    }
}
