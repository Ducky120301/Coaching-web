using System.Net;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Students;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class ChangeEmailTests(ApiFactory factory)
{
    private static async Task<StudentDetailDto> GetAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{id}"))!;

    // @BR-f01-p1-tai-khoan-hoc-vien-008 @BR-f01-p1-tai-khoan-hoc-vien-009: email chuẩn hóa; đăng nhập chuyển sang email mới
    // với mật khẩu tạm mới — email cũ và mật khẩu tạm cũ đều hết dùng được; Identity đổi cả Email lẫn UserName
    [Fact]
    public async Task Changing_email_of_sent_profile_moves_login_to_the_new_email()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        var oldTemp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        var newEmail = $"s{Guid.NewGuid():N}@test.local";

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, $" {newEmail.ToUpperInvariant()} ",
            (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(newEmail, (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Email);
        var newTemp = AccountRequests.TempPassword(factory.Outbox.All.Last(m => m.To == newEmail));
        Assert.NotEqual(oldTemp, newTemp);
        var client = factory.Capture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await AccountRequests.LoginAsync(client, student.Email, oldTemp)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AccountRequests.LoginAsync(client, newEmail, oldTemp)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.LoginAsync(client, newEmail, newTemp)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Null(await users.FindByEmailAsync(student.Email));
        Assert.Equal(newEmail, (await users.FindByEmailAsync(newEmail))!.UserName);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009: gửi tới email mới lỗi → hồ sơ VÀ tài khoản đăng nhập giữ email cũ, mật khẩu tạm cũ còn dùng được
    [Fact]
    public async Task Email_failure_keeps_the_old_email_in_profile_and_login()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        var oldTemp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        var newEmail = $"s{Guid.NewGuid():N}@test.local";
        factory.Outbox.FailNext = true;

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, newEmail, (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("account.email_send_failed", await StudentRequests.ErrorCodeAsync(response));
        var detail = await GetAsync(admin, student.Id);
        Assert.Equal(student.Email, detail.Email);
        Assert.Single(detail.Account.History);
        var login = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), student.Email, oldTemp);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Null(await users.FindByEmailAsync(newEmail));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-027: gửi lại đúng email hiện tại không phải lần sửa → không thêm dòng, không gửi thư
    [Fact]
    public async Task Same_email_is_a_no_op()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        var mailsBefore = factory.Outbox.All.Count(m => m.To == student.Email);

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, student.Email.ToUpperInvariant(),
            (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single((await GetAsync(admin, student.Id)).Account.History);
        Assert.Equal(mailsBefore, factory.Outbox.All.Count(m => m.To == student.Email));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-008: email của tài khoản coach → 409, hồ sơ giữ email cũ
    [Fact]
    public async Task Email_of_the_coach_account_is_taken()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, ApiFactory.AdminEmail,
            (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("student.email_taken", await StudentRequests.ErrorCodeAsync(response));
        Assert.Equal(student.Email, (await GetAsync(admin, student.Id)).Email);
    }
}
