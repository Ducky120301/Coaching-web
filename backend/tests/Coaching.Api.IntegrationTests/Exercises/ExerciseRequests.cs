using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Coaching.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

internal static class ExerciseRequests
{
    public const string Url = "/api/admin/exercises";
    public const string CategoriesUrl = "/api/admin/exercise-categories";

    public static async Task<CategoriesDto> CategoriesAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<CategoriesDto>(CategoriesUrl))!;

    /// <summary>Tên duy nhất trong DB dùng chung của collection (các test cùng coach admin).</summary>
    public static string UniqueName(string label) => $"{label} {Guid.NewGuid():N}";

    public static async Task<SaveExerciseCommand> CommandAsync(HttpClient client, string? name,
        string muscleGroup = "Ngực", string equipment = "Machine")
    {
        var catalog = await CategoriesAsync(client);
        return new SaveExerciseCommand(name,
            catalog.MuscleGroups.Single(c => c.Name == muscleGroup).Id,
            catalog.Equipment.Single(c => c.Name == equipment).Id,
            null, null, null, null, null, null, null, null, null, null);
    }

    public static async Task<ExerciseDetailDto> CreateAsync(HttpClient client, string name, string muscleGroup = "Ngực",
        string equipment = "Machine", Func<SaveExerciseCommand, SaveExerciseCommand>? change = null)
    {
        var command = await CommandAsync(client, name, muscleGroup, equipment);
        var response = await client.PostAsJsonAsync(Url, change is null ? command : change(command));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
    }

    public static async Task<ExerciseDetailDto> GetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<ExerciseDetailDto>($"{Url}/{id}"))!;

    public static SaveExerciseCommand ToCommand(ExerciseDetailDto d) =>
        new(d.Name, d.MuscleGroupId, d.EquipmentId, d.VideoUrl, d.Sets, d.RepsMin, d.RepsMax, d.RirMin, d.RirMax,
            d.RestSeconds, d.TechniqueNotes, d.CommonMistakes, d.Version);

    public static Task<HttpResponseMessage> UpdateAsync(HttpClient client, ExerciseDetailDto current,
        Func<SaveExerciseCommand, SaveExerciseCommand> change) =>
        client.PutAsJsonAsync($"{Url}/{current.Id}", change(ToCommand(current)));

    /// <summary>Đặt bài ở trạng thái ẩn thẳng trong DB (API ẩn bài có ở Task 4).</summary>
    public static async Task HideInDatabaseAsync(ApiFactory factory, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Exercises] SET [IsHidden] = 1 WHERE [Id] = {id}");
    }

    /// <summary>
    /// Coach mới (tài khoản Admin + hồ sơ coach). <paramref name="withCatalog"/> = true: tạo luôn danh mục ban đầu
    /// (giống lần khởi động kế tiếp); false: coach chưa có danh mục nào.
    /// </summary>
    public static async Task<(Guid CoachId, HttpClient Client)> NewCoachAsync(ApiFactory factory, bool withCatalog = true)
    {
        var email = $"coach-{Guid.NewGuid():N}@test.local";
        const string password = "Coach@12345";
        Guid coachId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sp = scope.ServiceProvider;
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser { UserName = email, Email = email, FullName = "Coach khác", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, Roles.Admin)).Succeeded);
            var db = sp.GetRequiredService<AppDbContext>();
            var coach = Coach.Create(user.Id, "Coach khác", Email.Create(email));
            db.Coaches.Add(coach);
            await db.SaveChangesAsync();
            coachId = coach.Id;
            if (withCatalog)
                await ExerciseCatalogSeeder.SeedAsync(db);
        }
        var tokens = await factory.LoginAsync(email, password, ClientApp.Admin);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (coachId, client);
    }

    public static async Task<HttpClient> StudentClientAsync(ApiFactory factory)
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    /// <summary>Client của admin seed gọi vào <paramref name="host"/> (host phụ dựng từ <paramref name="factory"/>).</summary>
    public static async Task<HttpClient> AdminClientAsync(ApiFactory factory, WebApplicationFactory<Program> host)
    {
        var tokens = await factory.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    /// <summary>
    /// Host phụ: ngay trước khi lưu một entity <typeparamref name="T"/> mới khớp <paramref name="match"/> (bước kiểm trùng
    /// đã qua), một request khác — scope + kết nối riêng, commit ngay — chạy <paramref name="competitor"/> đúng một lần.
    /// Lệnh INSERT của request chính vì vậy đụng chỉ mục duy nhất (2601).
    /// </summary>
    public static WebApplicationFactory<Program> WithRace<T>(ApiFactory factory, Race race, Func<T, bool> match,
        Func<IServiceProvider, T, Task> competitor) where T : class =>
        factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.ConfigureDbContext<AppDbContext>((sp, options) => options.AddInterceptors(
                new RaceInterceptor<T>(race, match, competitor, sp.GetRequiredService<IServiceScopeFactory>())))));

    private sealed class RaceInterceptor<T>(Race race, Func<T, bool> match, Func<IServiceProvider, T, Task> competitor,
        IServiceScopeFactory scopes) : SaveChangesInterceptor where T : class
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var entity = eventData.Context!.ChangeTracker.Entries<T>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .FirstOrDefault(match);
            if (entity is not null && race.TryClaim())
            {
                await using var scope = scopes.CreateAsyncScope();
                await competitor(scope.ServiceProvider, entity);
            }
            return result;
        }
    }
}

/// <summary>Trạng thái cuộc đua dùng chung giữa các DbContext của host phụ: chỉ chạy đối thủ một lần.</summary>
internal sealed class Race
{
    private int _claimed;

    public bool Happened => Volatile.Read(ref _claimed) == 1;

    public bool TryClaim() => Interlocked.Exchange(ref _claimed, 1) == 0;
}
