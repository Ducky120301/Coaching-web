using System.Net;
using System.Net.Http.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentCreateSteps(AcceptanceContext ctx, StudentApi api)
{
    // Background của ho-so-hoc-vien.feature
    [Given(@"^coach ""([^""]*)"" đã đăng nhập$")]
    public async Task CoachLoggedIn(string coachName)
    {
        Assert.Equal(AcceptanceContext.AdminName, coachName);
        await api.LoginCoachAsync();
    }

    // @BR-f02-ho-so-hoc-vien-001
    [Given(@"^chưa có hồ sơ nào dùng email ""([^""]*)""$")]
    public async Task NoStudentUsesEmail(string email)
    {
        var value = Email.Create(email);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Students.AnyAsync(s => s.Email == value));
    }

    // @BR-f02-ho-so-hoc-vien-001 @BR-f02-import-excel-025 ("một hồ sơ học viên" = hồ sơ đang coaching)
    [Given(@"^email ""([^""]*)"" đã thuộc về (một hồ sơ đang coaching|một hồ sơ học viên|tài khoản coach|một hồ sơ đã kết thúc coaching)$")]
    public async Task EmailBelongsTo(string email, string owner)
    {
        switch (owner)
        {
            case "một hồ sơ đang coaching":
            case "một hồ sơ học viên":
                await api.CreateOkAsync("Chủ email", email);
                break;
            case "một hồ sơ đã kết thúc coaching":
                var ended = await api.CreateOkAsync("Chủ email", email);
                await using (var scope = ctx.Host.Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [Students] SET [CoachingStatus] = N'Ended' WHERE [Id] = {ended.Id}");
                }
                break;
            default: // tài khoản coach
                await using (var scope = ctx.Host.Services.CreateAsyncScope())
                {
                    var sp = scope.ServiceProvider;
                    var users = sp.GetRequiredService<UserManager<AppUser>>();
                    var user = new AppUser { UserName = email, Email = email, FullName = "Coach khác", EmailConfirmed = true };
                    Assert.True((await users.CreateAsync(user, "Coach@12345")).Succeeded);
                    Assert.True((await users.AddToRoleAsync(user, Roles.Admin)).Succeeded);
                    var db = sp.GetRequiredService<AppDbContext>();
                    db.Coaches.Add(Coach.Create(user.Id, "Coach khác", Email.Create(email)));
                    await db.SaveChangesAsync();
                }
                break;
        }
    }

    [When(@"^coach tạo hồ sơ học viên với email ""([^""]*)""$")]
    public Task CreateWithEmail(string email) => api.TryCreateAsync("Học viên mới", email);

    [Then(@"^hồ sơ được tạo với email ""([^""]*)""$")]
    public void CreatedWithEmail(string email)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(email, api.LastDetail!.Email);
    }

    [Then("hồ sơ không được tạo vì email đã được dùng")]
    public Task RejectedEmailTaken() => api.AssertErrorAsync(HttpStatusCode.Conflict, "student.email_taken");

    // @BR-f02-ho-so-hoc-vien-002
    [When(@"^coach tạo hồ sơ học viên tên ""([^""]*)"" mà không có email$")]
    public Task CreateWithoutEmail(string fullName) => api.TryCreateAsync(fullName, null);

    [When(@"^coach tạo hồ sơ học viên tên ""([^""]*)"" với email ""([^""]*)""$")]
    public Task CreateWithNameAndEmail(string fullName, string email) => api.TryCreateAsync(fullName, email);

    [Then("hồ sơ không được lưu vì thiếu email")]
    public Task RejectedMissingEmail() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "email.required");

    [Then("hồ sơ không được lưu vì email không hợp lệ")]
    public Task RejectedInvalidEmail() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "email.invalid");

    // @BR-f02-ho-so-hoc-vien-003
    [When(@"^coach tạo hồ sơ học viên với họ tên chỉ gồm khoảng trắng và email ""([^""]*)""$")]
    public Task CreateWithBlankName(string email) => api.TryCreateAsync("   ", email);

    [Then("hồ sơ không được lưu vì thiếu họ tên")]
    public Task RejectedMissingName() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.full_name_required");

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007
    [When(@"^coach tạo hồ sơ học viên ""([^""]*)""$")]
    public Task CreateNamed(string fullName) => api.CreateOkAsync(fullName);

    // @BR-f02-ho-so-hoc-vien-005
    [Then(@"^tài khoản của ""([^""]*)"" ở trạng thái ""([^""]*)""$")]
    public async Task AccountStatusIs(string fullName, string label) =>
        Assert.Equal(StudentApi.AccountStatuses[label], (await api.GetAsync(fullName)).AccountStatus);

    // @BR-f02-ho-so-hoc-vien-006 (dùng lại ở BR-017→019, 022)
    [Then(@"^trạng thái coaching của ""([^""]*)"" là ""([^""]*)""$")]
    public async Task CoachingStatusIs(string fullName, string label) =>
        Assert.Equal(StudentApi.CoachingStatuses[label], (await api.GetAsync(fullName)).CoachingStatus);

    // @BR-f02-ho-so-hoc-vien-007
    [Then(@"^hồ sơ của ""([^""]*)"" thuộc coach ""([^""]*)""$")]
    public async Task BelongsToCoach(string fullName, string coachName)
    {
        var profile = await ctx.GetAsync("/api/admin/profile", api.AdminToken);
        var coach = (await profile.Content.ReadFromJsonAsync<CoachProfileDto>())!;
        Assert.Equal(coachName, coach.DisplayName);
        Assert.Equal(coach.Id, (await api.GetAsync(fullName)).CoachId);
    }
}
