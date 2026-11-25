using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class AccountSchemaTests(ApiFactory factory)
{
    // @ADR-f01-p1-tai-khoan-hoc-vien-006: tạo + đọc lại hồ sơ đi qua cột mới và bảng StudentAccountEvents
    [Fact]
    public async Task New_student_detail_has_not_sent_account_and_empty_history()
    {
        var admin = await factory.AdminClientAsync();
        var created = await StudentRequests.CreateAsync(admin, "Lan");

        Assert.Equal("NotSent", created.Account.State);
        Assert.Empty(created.Account.History);

        var loaded = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{created.Id}"))!;
        Assert.Equal("NotSent", loaded.Account.State);
        Assert.Null(loaded.Account.CredentialsSentAt);
        Assert.Null(loaded.Account.TempPasswordExpiresAt);
        Assert.Null(loaded.Account.ActivatedAt);
        Assert.Empty(loaded.Account.History);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-006: changeset v0.3.0 tạo đủ bảng/cột (AccountTokens dùng ở Task 3)
    [Fact]
    public async Task Changelog_v0_3_0_creates_account_tables_and_columns()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        async Task<List<string>> ColumnsOf(string table) => await db.Database
            .SqlQuery<string>($"SELECT COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {table}")
            .ToListAsync();

        var students = await ColumnsOf("Students");
        Assert.Contains("CredentialsSentAt", students);
        Assert.Contains("TempPasswordExpiresAt", students);
        Assert.Contains("ActivatedAt", students);
        Assert.Equal(
            new[] { "ActorUserId", "At", "FromEmail", "Id", "StudentId", "ToEmail", "Type" },
            (await ColumnsOf("StudentAccountEvents")).Order().ToList());
        Assert.Equal(
            new[] { "ConsumedAt", "CreatedAt", "ExpiresAt", "Id", "Purpose", "RevokedAt", "Source", "TokenHash", "UserId" },
            (await ColumnsOf("AccountTokens")).Order().ToList());
    }
}
