using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Accounts.Commands;
using Coaching.Application.Auth;
using Coaching.Application.Accounts;
using Coaching.Application.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class PasswordResetTests(ApiFactory factory)
{
    private static async Task<AuthTokens> LoginTokensAsync(HttpClient client, string email, string password)
    {
        var response = await AccountRequests.LoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-023 @ADR-f01-p1-tai-khoan-hoc-vien-005: học viên đã kích hoạt, email lạ, học viên Chờ kích hoạt,
    // email coach → cùng status, cùng body từng byte (NFR-f01-p1-tai-khoan-hoc-vien-002, phần không đo thời gian)
    [Fact]
    public async Task Forgot_password_answers_identically_for_every_email_kind()
    {
        var activated = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        var pending = await AccountRequests.CreateCompleteAsync(admin);
        string[] emails = [activated.Email, $"la-{Guid.NewGuid():N}@test.local", pending.Email, ApiFactory.AdminEmail];
        var client = factory.Capture.CreateClient();

        var answers = new List<(HttpStatusCode Status, string Body)>();
        foreach (var email in emails)
        {
            var response = await AccountRequests.ForgotAsync(client, email);
            answers.Add((response.StatusCode, await response.Content.ReadAsStringAsync()));
        }

        Assert.All(answers, a => Assert.Equal(HttpStatusCode.Accepted, a.Status));
        Assert.Single(answers.Select(a => a.Body).Distinct());
        using var json = JsonDocument.Parse(answers[0].Body);
        Assert.Equal(ForgotPasswordResponse.GenericMessage, json.RootElement.GetProperty("message").GetString());
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-005 — seed LOẠI của BR-f01-p1-tai-khoan-hoc-vien-027: học viên tự xin link → lịch sử tài khoản không thêm dòng
    [Fact]
    public async Task Self_forgot_request_adds_no_account_history_line()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        var before = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!.Account.History.Count;

        var response = await AccountRequests.ForgotAsync(factory.Capture.CreateClient(), student.Email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await AccountRequests.WaitForMailsAsync(factory.Outbox, student.Email, AccountRequests.ResetSubject, 1);
        var after = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!.Account.History.Count;
        Assert.Equal(before, after);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-021 @ADR-f01-p1-tai-khoan-hoc-vien-009: đặt lại xong → mọi refresh token bị thu hồi với lý do PasswordReset
    [Fact]
    public async Task Password_reset_revokes_every_session()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory, "Coach2025");
        var client = factory.Capture.CreateClient();
        var phone = await LoginTokensAsync(client, student.Email, "Coach2025");
        var laptop = await LoginTokensAsync(client, student.Email, "Coach2025");
        var admin = await factory.CaptureAdminClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.SendResetLinkAsync(admin, student.Id)).StatusCode);
        var token = AccountRequests.ResetToken(
            factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == AccountRequests.ResetSubject));

        var reset = await AccountRequests.ResetAsync(client, token, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        foreach (var session in new[] { phone, laptop })
        {
            var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        var sessions = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
        Assert.NotEmpty(sessions);
        Assert.All(sessions, t => Assert.Equal(RefreshTokenRevokedReason.PasswordReset, t.RevokedReason));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-020: link tự xin mới hủy link coach gửi trước đó
    [Fact]
    public async Task New_self_link_supersedes_the_coach_link()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        var client = factory.Capture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.SendResetLinkAsync(admin, student.Id)).StatusCode);
        var coachToken = AccountRequests.ResetToken(
            factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == AccountRequests.ResetSubject));
        Assert.Equal(HttpStatusCode.Accepted, (await AccountRequests.ForgotAsync(client, student.Email)).StatusCode);
        var selfToken = AccountRequests.ResetToken(
            (await AccountRequests.WaitForMailsAsync(factory.Outbox, student.Email, AccountRequests.ResetSubject, 2))[^1]);

        var withCoachLink = await AccountRequests.ResetAsync(client, coachToken, "Coach2026", "Coach2026");
        var withSelfLink = await AccountRequests.ResetAsync(client, selfToken, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.BadRequest, withCoachLink.StatusCode);
        Assert.Equal("auth.reset_link_invalid", await StudentRequests.ErrorCodeAsync(withCoachLink));
        Assert.Equal(HttpStatusCode.NoContent, withSelfLink.StatusCode);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-017: coach gửi link đặt lại mà dịch vụ email lỗi → 503,
    // rollback hết: không thêm dòng ResetLinkSent, không có token mới, link cũ (đã bị thu hồi trong giao dịch) vẫn dùng được
    [Fact]
    public async Task Reset_link_email_failure_rolls_back_history_token_and_revocation()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.SendResetLinkAsync(admin, student.Id)).StatusCode);
        var linkA = AccountRequests.ResetToken(
            factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == AccountRequests.ResetSubject));
        var historyBefore = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!.Account.History;
        var resetEventsBefore = historyBefore.Count(e => e.Type == nameof(AccountEventType.ResetLinkSent));
        var tokensBefore = await ResetTokenIdsAsync(student.Id);
        var mailsBefore = factory.Outbox.All.Count(m => m.To == student.Email);

        factory.Outbox.FailNext = true;
        var response = await AccountRequests.SendResetLinkAsync(admin, student.Id);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("account.email_send_failed", await StudentRequests.ErrorCodeAsync(response));
        var historyAfter = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!.Account.History;
        Assert.Equal(resetEventsBefore, historyAfter.Count(e => e.Type == nameof(AccountEventType.ResetLinkSent)));
        Assert.Equal(historyBefore.Count, historyAfter.Count);
        Assert.Equal(tokensBefore, await ResetTokenIdsAsync(student.Id));
        Assert.Equal(mailsBefore, factory.Outbox.All.Count(m => m.To == student.Email));

        var reset = await AccountRequests.ResetAsync(factory.Capture.CreateClient(), linkA, "Coach2026", "Coach2026");
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
    }

    private async Task<List<Guid>> ResetTokenIdsAsync(Guid studentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Students.Where(s => s.Id == studentId).Select(s => s.UserId).SingleAsync();
        return await db.AccountTokens
            .Where(t => t.UserId == userId && t.Purpose == AccountTokenPurpose.PasswordReset)
            .OrderBy(t => t.Id).Select(t => t.Id).ToListAsync();
    }
}
