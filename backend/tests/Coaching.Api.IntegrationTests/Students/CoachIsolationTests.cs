using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Application.Students.Queries;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class CoachIsolationTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-007: coach chỉ thấy/sửa học viên của mình; học viên coach khác → 404, không có trong danh sách
    [Fact]
    public async Task Coach_cannot_read_or_change_another_coachs_student()
    {
        var other = await OtherCoachClientAsync();
        var theirs = await StudentRequests.CreateAsync(other, "Học viên của coach khác");
        var admin = await factory.AdminClientAsync();
        var url = $"{StudentRequests.Url}/{theirs.Id}";

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync(url,
            new { fullName = "Bị sửa", registration = new RegistrationInput(), version = theirs.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{url}/internal-note",
            new { note = "x", version = theirs.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"{url}/coaching-status",
            new { status = "Paused", reason = "x", version = theirs.Version })).StatusCode);

        var list = (await admin.GetFromJsonAsync<List<StudentListItemDto>>(StudentRequests.Url))!;
        Assert.DoesNotContain(list, r => r.Id == theirs.Id);
        // dữ liệu của học viên kia không bị đổi
        var intact = (await other.GetFromJsonAsync<StudentDetailDto>(url))!;
        Assert.Equal("Học viên của coach khác", intact.FullName);
        Assert.Equal("Active", intact.CoachingStatus);
    }

    private async Task<HttpClient> OtherCoachClientAsync()
    {
        var email = $"coach-{Guid.NewGuid():N}@test.local";
        const string password = "Coach@12345";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sp = scope.ServiceProvider;
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser { UserName = email, Email = email, FullName = "Coach khác", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, Roles.Admin)).Succeeded);
            var db = sp.GetRequiredService<AppDbContext>();
            db.Coaches.Add(Coach.Create(user.Id, "Coach khác", Email.Create(email)));
            await db.SaveChangesAsync();
        }
        var tokens = await factory.LoginAsync(email, password, ClientApp.Admin);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }
}
