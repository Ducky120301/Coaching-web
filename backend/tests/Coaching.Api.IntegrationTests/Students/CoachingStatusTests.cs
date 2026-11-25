using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class CoachingStatusTests(ApiFactory factory)
{
    private static string StatusUrl(Guid id) => $"{StudentRequests.Url}/{id}/coaching-status";

    // @BR-f02-ho-so-hoc-vien-015 (seed loại khỏi living doc; cần endpoint của task này):
    // hai lệnh đổi trạng thái cùng một phiên bản → chỉ một thành công, lịch sử 1 dòng
    [Fact]
    public async Task Two_status_changes_from_the_same_version_only_one_succeeds()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var first = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status = "Paused", reason = "Đi công tác", version = student.Version });
        var second = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status = "Ended", reason = "Hết gói", version = student.Version });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("student.version_conflict", await StudentRequests.ErrorCodeAsync(second));
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Equal("Paused", detail.CoachingStatus);
        Assert.Single(detail.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-018 (seed loại khỏi living doc): lý do 501 ký tự
    [Fact]
    public async Task Reason_longer_than_500_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PostAsJsonAsync(StatusUrl(student.Id),
            new { status = "Paused", reason = new string('a', 501), version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.reason_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-020 (seed loại khỏi living doc): đổi bị từ chối không thêm lịch sử
    [Fact]
    public async Task Rejected_change_adds_no_history()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PostAsJsonAsync(StatusUrl(student.Id),
            new { status = "Active", reason = "Nhầm", version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.status_unchanged", await StudentRequests.ErrorCodeAsync(response));
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Empty(detail.StatusHistory);
    }

    [Theory]
    [InlineData("Archived")]
    [InlineData("1")]
    public async Task Unknown_status_is_rejected(string status)
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status, reason = "x", version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.status_invalid", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-021 (seed loại khỏi living doc): không có API sửa/xóa lịch sử
    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task History_cannot_be_edited_or_deleted(string method)
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), StatusUrl(student.Id)));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // NFR-f02-ho-so-hoc-vien-002: mỗi lần đổi trạng thái có đúng 1 dòng audit (người + thời điểm), không lưu lý do/ghi chú
    [Fact]
    public async Task Status_change_is_audited_without_reason_or_note()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);
        var noted = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}/internal-note",
            new { note = "Ghi chú bí mật", version = student.Version });
        var version = (await noted.Content.ReadFromJsonAsync<StudentDetailDto>())!.Version;

        var changed = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status = "Paused", reason = "Lý do riêng tư", version });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = Assert.Single(await db.AuditLogs
            .Where(a => a.EntityType == "Student" && a.EntityId == student.Id.ToString())
            .ToListAsync());
        Assert.Equal("coaching_status.changed", entry.Action);
        Assert.Equal("Active→Paused", entry.Details);
        Assert.Equal(ApiFactory.AdminEmail, entry.ActorEmail);
        Assert.NotEqual(default, entry.OccurredAt);
        Assert.DoesNotContain("Lý do riêng tư", entry.Details);
        Assert.DoesNotContain("Ghi chú bí mật", entry.Details);
    }
}
