using System.Net;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class TempPasswordLoginTests(ApiFactory factory)
{
    private async Task<(Guid StudentId, Guid UserId, string Email, string Temp)> SentAsync()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        var temp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        return (student.Id, userId, student.Email, temp);
    }

    private async Task SetTempExpiryAsync(Guid studentId, DateTimeOffset expiresAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Students] SET [TempPasswordExpiresAt] = {expiresAt} WHERE [Id] = {studentId}");
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-002: mật khẩu tạm còn hạn → 200 có mã kích hoạt, không có phiên nào được cấp
    [Fact]
    public async Task Valid_temp_password_returns_activation_challenge_without_session()
    {
        var (_, userId, email, temp) = await SentAsync();
        var before = DateTimeOffset.UtcNow;

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("activationRequired").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("activationToken").GetString()));
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        var expiresAt = json.RootElement.GetProperty("activationTokenExpiresAt").GetDateTimeOffset();
        Assert.InRange(expiresAt, before.AddMinutes(29), DateTimeOffset.UtcNow.AddMinutes(30));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == userId));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-002: mật khẩu tạm trên web quản trị → 401 chung, không phải 403 WrongApp
    [Fact]
    public async Task Temp_password_on_admin_web_returns_generic_401()
    {
        var (_, _, email, temp) = await SentAsync();

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp, "admin");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.invalid_credentials", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-012 @ADR-f01-p1-tai-khoan-hoc-vien-002: hết hạn → 401 chung và tính là một lần sai
    [Fact]
    public async Task Expired_temp_password_is_generic_401_and_counts_as_failed_attempt()
    {
        var (studentId, userId, email, temp) = await SentAsync();
        await SetTempExpiryAsync(studentId, DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.invalid_credentials", await StudentRequests.ErrorCodeAsync(response));
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var failed = (await users.FindByIdAsync(userId.ToString()))!.AccessFailedCount;
        Assert.Equal(1, failed);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-002: mã kích hoạt không sống quá hạn mật khẩu tạm
    [Fact]
    public async Task Activation_token_never_outlives_the_temp_password()
    {
        var (studentId, _, email, temp) = await SentAsync();
        var tempExpiry = DateTimeOffset.UtcNow.AddMinutes(10);
        await SetTempExpiryAsync(studentId, tempExpiry);

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(tempExpiry, json.RootElement.GetProperty("activationTokenExpiresAt").GetDateTimeOffset());
    }
}
