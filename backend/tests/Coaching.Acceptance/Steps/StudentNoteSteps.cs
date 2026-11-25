using System.Net;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentNoteSteps(AcceptanceContext ctx, StudentApi api)
{
    private string? _note;
    private string? _selfJson;

    // @BR-f02-ho-so-hoc-vien-014
    [When(@"^coach sửa ghi chú nội bộ của ""([^""]*)""$")]
    public Task EditNote(string fullName) => api.UpdateNoteOkAsync(fullName, $"Ghi chú {Guid.NewGuid():N}");

    [Then(@"^ghi chú nội bộ của ""([^""]*)"" cho biết sửa lần cuối ngày ""([^""]*)""$")]
    [Then(@"^ghi chú nội bộ của ""([^""]*)"" vẫn cho biết sửa lần cuối ngày ""([^""]*)""$")]
    public async Task NoteEditedOn(string fullName, string date)
    {
        var detail = await api.GetAsync(fullName);
        Assert.NotNull(detail.InternalNoteUpdatedAt);
        Assert.Equal(StepText.ParseDate(date), StepText.VietnamDate(detail.InternalNoteUpdatedAt!.Value));
    }

    [Given(@"^ghi chú nội bộ của ""([^""]*)"" sửa lần cuối ngày ""([^""]*)""$")]
    public async Task NoteEditedEarlier(string fullName, string date)
    {
        await api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));
        await api.EnsureAsync(fullName);
        await api.UpdateNoteOkAsync(fullName, "Ghi chú ban đầu");
    }

    // @BR-f02-ho-so-hoc-vien-016
    [Given(@"^ghi chú nội bộ của ""([^""]*)"" là ""([^""]*)""$")]
    public async Task NoteIs(string fullName, string note)
    {
        await api.EnsureAsync(fullName);
        await api.UpdateNoteOkAsync(fullName, note);
        _note = note;
    }

    [When(@"^coach xem hồ sơ của ""([^""]*)""$")]
    public Task CoachViews(string fullName) => api.GetAsync(fullName);

    [Then(@"^coach thấy ghi chú nội bộ ""([^""]*)""$")]
    public void CoachSeesNote(string note) => Assert.Equal(note, api.LastDetail!.InternalNote);

    [When(@"^""([^""]*)"" lấy dữ liệu của chính mình$")]
    public async Task StudentFetchesOwnData(string fullName)
    {
        var detail = await api.GetAsync(fullName);
        const string password = "Student@123";
        await using (var scope = ctx.Host.Services.CreateAsyncScope())
        {
            // Đặt mật khẩu riêng + kích hoạt trực tiếp để học viên đăng nhập được
            // (F01-P1: tài khoản Chờ kích hoạt chỉ đăng nhập bằng mật khẩu tạm — ADR-f01-p1-tai-khoan-hoc-vien-002).
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByEmailAsync(detail.Email))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.Students.SingleAsync(s => s.UserId == user.Id);
            profile.Activate(scope.ServiceProvider.GetRequiredService<IClock>().UtcNow);
            await db.SaveChangesAsync();
        }

        var tokens = await ctx.LoginOkAsync(detail.Email, password, "client");
        var response = await ctx.GetAsync("/api/client/me", tokens.AccessToken);
        _selfJson = await response.Content.ReadAsStringAsync();
    }

    [Then(@"^dữ liệu ""([^""]*)"" nhận được không chứa ghi chú nội bộ$")]
    public void OwnDataHasNoNote(string fullName)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var names = new List<string>();
        var values = new List<string>();
        using (var json = JsonDocument.Parse(_selfJson!))
            Collect(json.RootElement, names, values);

        Assert.Contains(fullName, values); // đúng dữ liệu của học viên
        Assert.DoesNotContain(names, n => n.Contains("note", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(_note!, values);
    }

    private static void Collect(JsonElement element, List<string> names, List<string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    names.Add(property.Name);
                    Collect(property.Value, names, values);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    Collect(item, names, values);
                break;
            case JsonValueKind.String:
                values.Add(element.GetString()!);
                break;
        }
    }
}
