using System.Net.Http.Json;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Queries;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Coaching.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseLibrarySeedTests(ApiFactory factory)
{
    /// <summary>Khởi động (rồi tắt) một host trên cùng DB → DatabaseInitializer chạy lại.</summary>
    private async Task StartAsync(string environment, bool seedLibrary)
    {
        await using var host = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment(environment);
            b.UseSetting("Email:Provider", "Log"); // Capture bị cấm ngoài dev/e2e (ADR-f01-p1-tai-khoan-hoc-vien-007)
            b.UseSetting(ExerciseLibrarySeeder.EnabledKey, seedLibrary ? "true" : "false");
        });
        _ = host.Services;
    }

    private async Task<int> CountAsync(Guid coachId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Exercises.CountAsync(e => e.CoachId == coachId);
    }

    private static async Task<ExerciseListItemDto> FindAsync(HttpClient client, string name) =>
        Assert.Single((await client.GetFromJsonAsync<List<ExerciseListItemDto>>(
                $"{ExerciseRequests.Url}?q={Uri.EscapeDataString(name)}"))!, r => r.Name == name);

    // @ADR-f03-thu-vien-bai-tap-007: file nhúng có đủ 99 bài, đã gán theo danh mục ban đầu (Phụ lục), không trùng (tên, dụng cụ), đều có video
    [Fact]
    public void ADR007_Seed_file_has_99_exercises_mapped_to_the_default_catalog()
    {
        var seeds = ExerciseLibrarySeeder.Load();

        Assert.Equal(99, seeds.Count);
        Assert.All(seeds, s =>
        {
            Assert.Contains(s.MuscleGroup, DefaultCatalog.MuscleGroups);
            Assert.Contains(s.Equipment, DefaultCatalog.Equipment);
            Assert.NotNull(s.VideoUrl);
        });
        Assert.Equal(99, seeds.Select(s => (ExerciseNameKey.For(s.Name), s.Equipment)).Distinct().Count());
        Assert.Equal(27, seeds.Count(s => s.Sets is not null));
    }

    // @ADR-f03-thu-vien-bai-tap-007: (a) coach thư viện trống nhận 99 bài ở host Production; (b) khởi động lại không nhân đôi;
    // (c) coach đã có ≥ 1 bài không được nạp; coach đã xoá hết bài sau khi nạp cũng không được nạp lại
    [Fact]
    public async Task ADR007_Empty_library_gets_99_exercises_once_in_every_environment()
    {
        var admin = await factory.AdminClientAsync();
        await ExerciseRequests.CreateAsync(admin, ExerciseRequests.UniqueName("Giữ thư viện admin")); // admin không trống → không bị nạp
        var (emptyCoach, emptyClient) = await ExerciseRequests.NewCoachAsync(factory, withCatalog: false);
        var (busyCoach, busyClient) = await ExerciseRequests.NewCoachAsync(factory);
        await ExerciseRequests.CreateAsync(busyClient, "Bài riêng của coach");

        await StartAsync("Production", seedLibrary: true);

        Assert.Equal(99, await CountAsync(emptyCoach)); // (a) — danh mục ban đầu được tạo trước ở cùng lần khởi động
        Assert.Equal(1, await CountAsync(busyCoach)); // (c)
        var hip = await FindAsync(emptyClient, "Hip Adduction");
        Assert.Equal(("Đùi trước", "Machine", true), (hip.MuscleGroup, hip.Equipment, hip.HasVideo));
        var crunch = await FindAsync(emptyClient, "Decline Crunch");
        Assert.Equal(("Bụng", "Bodyweight"), (crunch.MuscleGroup, crunch.Equipment));
        var incline = await ExerciseRequests.GetAsync(emptyClient, (await FindAsync(emptyClient, "45° Incline Chest Fly")).Id);
        Assert.Equal(new int?[] { 3, 10, 12, 1, 1, 90 },
            new[] { incline.Sets, incline.RepsMin, incline.RepsMax, incline.RirMin, incline.RirMax, incline.RestSeconds });
        Assert.Equal("https://www.youtube.com/shorts/PfmfAeMpym4", incline.VideoUrl);
        var facepull = await ExerciseRequests.GetAsync(emptyClient, (await FindAsync(emptyClient, "Facepull with External Rotation")).Id);
        Assert.Equal(("Vai sau", 0, 1), (facepull.MuscleGroup, facepull.RirMin!.Value, facepull.RirMax!.Value));
        Assert.Null((await ExerciseRequests.GetAsync(emptyClient, (await FindAsync(emptyClient, "Barbell Bench Press")).Id)).Sets);

        await StartAsync("Production", seedLibrary: true);
        Assert.Equal(99, await CountAsync(emptyCoach)); // (b)

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Exercises] WHERE [CoachId] = {emptyCoach}");
        }
        await StartAsync("Staging", seedLibrary: true); // không dùng Development: appsettings.Development.json bật DemoSeed
        Assert.Equal(0, await CountAsync(emptyCoach)); // đã nạp một lần → không bao giờ nạp lại
        Assert.Equal(1, await CountAsync(busyCoach));
    }

    // @ADR-f03-thu-vien-bai-tap-007: Seed:ExerciseLibrary=false (host test) → không nạp, không đánh dấu
    [Fact]
    public async Task ADR007_Seed_is_skipped_when_disabled_by_configuration()
    {
        var (coachId, _) = await ExerciseRequests.NewCoachAsync(factory);

        await StartAsync("Production", seedLibrary: false);

        Assert.Equal(0, await CountAsync(coachId));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Set<ExerciseLibrarySeed>().AnyAsync(s => s.CoachId == coachId));
    }
}
