using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Coaching.Application.Students;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class InternalNoteTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-014: sửa ghi chú có thời điểm riêng; sửa form sau đó không đổi thời điểm ghi chú
    [Fact]
    public async Task Note_time_changes_only_when_note_changes()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);
        var url = $"{StudentRequests.Url}/{student.Id}";

        var noted = await admin.PutAsJsonAsync($"{url}/internal-note", new { note = "Theo dõi vai trái", version = student.Version });
        Assert.Equal(HttpStatusCode.OK, noted.StatusCode);
        var afterNote = (await noted.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("Theo dõi vai trái", afterNote.InternalNote);
        Assert.NotNull(afterNote.InternalNoteUpdatedAt);

        var edited = await admin.PutAsJsonAsync(url,
            new { fullName = student.FullName, registration = new RegistrationInput { TrainingGoal = "Tăng cơ" }, version = afterNote.Version });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal(afterNote.InternalNoteUpdatedAt, (await edited.Content.ReadFromJsonAsync<StudentDetailDto>())!.InternalNoteUpdatedAt);
    }

    // @ADR-f02-ho-so-hoc-vien-009: ghi chú 5001 ký tự → 400
    [Fact]
    public async Task Note_longer_than_5000_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}/internal-note",
            new { note = new string('a', 5001), version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.internal_note_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f02-ho-so-hoc-vien-007: /api/client/me đọc từ Student của chính học viên trong token
    [Fact]
    public async Task Client_me_returns_own_student_profile()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var me = (await client.GetFromJsonAsync<StudentSelfProbe>("/api/client/me"))!;

        Assert.Equal(email, me.Email);
        Assert.Equal("Học viên Test", me.FullName);
        Assert.Equal("Active", me.CoachingStatus);
        Assert.Equal("Activated", me.AccountStatus); // F01-P1: CreateStudentAsync tạo học viên có mật khẩu riêng → Đã kích hoạt
    }

    // @BR-f02-ho-so-hoc-vien-014 @BR-f02-ho-so-hoc-vien-016: tạo hồ sơ kèm ghi chú trong cùng request
    // → coach thấy ghi chú + thời điểm sửa ghi chú; học viên vẫn không nhận được ghi chú
    [Fact]
    public async Task Create_with_note_stores_note_but_client_never_sees_it()
    {
        var admin = await factory.AdminClientAsync();
        var email = StudentRequests.NewEmail();

        var created = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email, internalNote = "  Tránh lunge sâu vì gối phải  " });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = (await created.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("Tránh lunge sâu vì gối phải", dto.InternalNote);
        Assert.Equal(dto.CreatedAt, dto.InternalNoteUpdatedAt);

        const string password = "Student@123";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            // Đặt mật khẩu riêng + kích hoạt trực tiếp để học viên đăng nhập được
            // (F01-P1: tài khoản Chờ kích hoạt chỉ đăng nhập bằng mật khẩu tạm — ADR-f01-p1-tai-khoan-hoc-vien-002).
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.Students.SingleAsync(s => s.UserId == user.Id);
            profile.Activate(scope.ServiceProvider.GetRequiredService<IClock>().UtcNow);
            await db.SaveChangesAsync();
        }
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var json = await client.GetStringAsync("/api/client/me");

        Assert.DoesNotContain("note", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lunge", json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record StudentSelfProbe(string FullName, string Email, string CoachingStatus, string AccountStatus);
}
