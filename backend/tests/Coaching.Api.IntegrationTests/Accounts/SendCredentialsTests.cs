using System.Net;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class SendCredentialsTests(ApiFactory factory)
{
    // @BR-f01-p1-tai-khoan-hoc-vien-025: kết quả gửi và hồ sơ coach xem không bao giờ chứa mật khẩu tạm trong thư
    [Fact]
    public async Task Response_and_profile_never_contain_the_temporary_password()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);

        var sent = await AccountRequests.SendCredentialsAsync(admin, student.Id);

        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var temp = AccountRequests.TempPassword(factory.Outbox.All.Last(m => m.To == student.Email));
        Assert.DoesNotContain(temp, await sent.Content.ReadAsStringAsync());
        Assert.DoesNotContain(temp, await admin.GetStringAsync($"{StudentRequests.Url}/{student.Id}"));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu tạm là mật khẩu Identity — DB chỉ có hash
    [Fact]
    public async Task Temporary_password_is_stored_only_as_the_identity_hash()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);

        var temp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        var user = (await users.FindByIdAsync(userId.ToString()))!;
        Assert.NotNull(user.PasswordHash);
        Assert.DoesNotContain(temp, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, temp));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-006 @ADR-f01-p1-tai-khoan-hoc-vien-004: dịch vụ email lỗi → 503, rollback hết
    [Fact]
    public async Task Email_failure_rolls_back_the_send()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        factory.Outbox.FailNext = true;

        var response = await AccountRequests.SendCredentialsAsync(admin, student.Id);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("account.email_send_failed", await StudentRequests.ErrorCodeAsync(response));
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Equal("NotSent", detail.Account.State);
        Assert.Null(detail.Account.TempPasswordExpiresAt);
        Assert.Empty(detail.Account.History);
        Assert.DoesNotContain(factory.Outbox.All, m => m.To == student.Email);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        Assert.False(await users.HasPasswordAsync((await users.FindByIdAsync(userId.ToString()))!));
    }
}
