using System.Security.Cryptography;
using System.Text;
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class AccountTokenStoreTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);

    private readonly MutableTimeProvider _time = new(Start);
    private readonly WebApplicationFactory<Program> _host;

    public AccountTokenStoreTests(ApiFactory factory) =>
        _host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_time);
        }));

    public void Dispose() => _host.Dispose();

    // @ADR-f01-p1-tai-khoan-hoc-vien-003 @NFR-f01-p1-tai-khoan-hoc-vien-001: trả token gốc 32 byte base64url, DB chỉ có SHA-256
    [Fact]
    public async Task Issue_returns_raw_token_and_stores_only_its_hash()
    {
        var userId = await NewUserAsync();

        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));

        Assert.Equal(43, raw.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", raw);
        var row = await Db(db => db.AccountTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))), row.TokenHash);
        Assert.NotEqual(raw, row.TokenHash);
        Assert.Equal(AccountTokenPurpose.Activation, row.Purpose);
        Assert.Equal(AccountTokenSource.Login, row.Source);
        Assert.Equal(Start, row.CreatedAt);
        Assert.Equal(Start.AddMinutes(30), row.ExpiresAt);
        Assert.Null(row.ConsumedAt);
        Assert.Null(row.RevokedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: hết hạn khi now >= ExpiresAt (đúng mốc là hết hạn)
    [Fact]
    public async Task Token_is_valid_until_just_before_expiry_and_invalid_at_expiry()
    {
        var userId = await NewUserAsync();
        var expiresAt = Start.AddMinutes(30);
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, expiresAt));

        _time.Set(expiresAt.AddSeconds(-1));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));

        _time.Set(expiresAt);
        Assert.Null(await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: token chỉ hợp lệ cho đúng mục đích; token rỗng/lạ không hợp lệ
    [Fact]
    public async Task Wrong_purpose_blank_or_unknown_token_is_invalid()
    {
        var userId = await NewUserAsync();
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));

        Assert.Null(await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));
        Assert.Null(await Tokens(t => t.FindValidAsync(null, AccountTokenPurpose.Activation)));
        Assert.Null(await Tokens(t => t.FindValidAsync("", AccountTokenPurpose.Activation)));
        Assert.Null(await Tokens(t => t.FindValidAsync("khong-phai-token", AccountTokenPurpose.Activation)));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: dùng một lần — lần tiêu đầu trả true; tiêu lại trả false, không đổi mốc
    [Fact]
    public async Task Consume_succeeds_once_then_returns_false_without_changing_consumed_at()
    {
        var userId = await NewUserAsync();
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(30)));

        Assert.True(await Tokens(t => t.ConsumeAsync(raw)));
        _time.Set(Start.AddMinutes(1));
        Assert.False(await Tokens(t => t.ConsumeAsync(raw)));

        Assert.Null(await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));
        var row = await Db(db => db.AccountTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));
        Assert.Equal(Start, row.ConsumedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: token đã bị hủy (vd. coach gửi lại thông tin) không tiêu được
    [Fact]
    public async Task Consume_on_revoked_token_returns_false_and_leaves_it_unconsumed()
    {
        var userId = await NewUserAsync();
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));
        await Tokens(t => t.RevokeAllAsync(userId, AccountTokenPurpose.Activation));

        Assert.False(await Tokens(t => t.ConsumeAsync(raw)));

        var row = await Db(db => db.AccountTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));
        Assert.Null(row.ConsumedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: token hết hạn (now >= ExpiresAt) không tiêu được
    [Fact]
    public async Task Consume_on_expired_token_returns_false()
    {
        var userId = await NewUserAsync();
        var expiresAt = Start.AddMinutes(30);
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, expiresAt));
        _time.Set(expiresAt);

        Assert.False(await Tokens(t => t.ConsumeAsync(raw)));

        var row = await Db(db => db.AccountTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));
        Assert.Null(row.ConsumedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: token mới hủy token cũ cùng user + mục đích, không đụng mục đích khác
    [Fact]
    public async Task Issuing_revokes_previous_live_token_of_same_purpose_only()
    {
        var userId = await NewUserAsync();
        var first = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(30)));
        var activation = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));
        _time.Set(Start.AddMinutes(5));

        var second = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, Start.AddMinutes(35)));

        Assert.Null(await Tokens(t => t.FindValidAsync(first, AccountTokenPurpose.PasswordReset)));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(second, AccountTokenPurpose.PasswordReset)));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(activation, AccountTokenPurpose.Activation)));
        var revoked = await Db(db => db.AccountTokens.AsNoTracking()
            .SingleAsync(t => t.UserId == userId && t.Purpose == AccountTokenPurpose.PasswordReset && t.RevokedAt != null));
        Assert.Equal(Start.AddMinutes(5), revoked.RevokedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: hủy hết token của một mục đích (gửi lại thông tin, sửa email)
    [Fact]
    public async Task RevokeAll_revokes_only_the_given_purpose()
    {
        var userId = await NewUserAsync();
        var activation = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));
        var reset = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(30)));

        await Tokens(t => t.RevokeAllAsync(userId, AccountTokenPurpose.Activation));

        Assert.Null(await Tokens(t => t.FindValidAsync(activation, AccountTokenPurpose.Activation)));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(reset, AccountTokenPurpose.PasswordReset)));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: thời điểm phát token gần nhất theo user + mục đích + nguồn (dùng cho giới hạn 60 giây)
    [Fact]
    public async Task LastIssuedAt_returns_latest_issue_time_per_purpose_and_source()
    {
        var userId = await NewUserAsync();
        Assert.Null(await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self)));

        await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, Start.AddMinutes(30)));
        _time.Set(Start.AddMinutes(2));
        await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(32)));

        Assert.Equal(Start, await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self)));
        Assert.Equal(Start.AddMinutes(2), await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach)));

        _time.Set(Start.AddMinutes(3));
        await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, Start.AddMinutes(33)));
        Assert.Equal(Start.AddMinutes(3), await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self)));
        Assert.Null(await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Self)));
    }

    private async Task<T> Tokens<T>(Func<IAccountTokens, Task<T>> action)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IAccountTokens>());
    }

    private async Task Tokens(Func<IAccountTokens, Task> action)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IAccountTokens>());
    }

    private async Task<T> Db<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task<Guid> NewUserAsync()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = $"token-{Guid.NewGuid():N}@test.local";
        var user = new AppUser { UserName = email, Email = email, FullName = "Token Test" };
        var created = await users.CreateAsync(user);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        return user.Id;
    }

    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public void Set(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
