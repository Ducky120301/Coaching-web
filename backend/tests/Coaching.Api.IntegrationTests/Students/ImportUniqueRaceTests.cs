using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Coaching.Application.Students.Import;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportUniqueRaceTests(ApiFactory factory)
{
    // @BR-f02-import-excel-025 @BR-f02-import-excel-033 @BR-f02-import-excel-034: dòng 3 thua cuộc đua email với một
    // request khác (tài khoản cùng email được tạo sau bước kiểm trùng, chỉ mục duy nhất chặn) → chỉ dòng 3 bị bỏ;
    // các dòng sau trong cùng file vẫn được tạo, không bị báo nhầm "email đã có hồ sơ".
    [Fact]
    public async Task Unique_violation_on_one_row_does_not_poison_the_following_rows()
    {
        var race = new RaceOnInsert();
        using var racing = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.ConfigureDbContext<AppDbContext>((sp, options) =>
                options.AddInterceptors(new RaceInterceptor(race, sp.GetRequiredService<IServiceScopeFactory>())))));
        var admin = racing.CreateClient();
        var tokens = await factory.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        admin.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var before = ImportRequests.NewEmail();
        var raced = $"{RaceOnInsert.Prefix}{Guid.NewGuid():N}@test.local";
        var after1 = ImportRequests.NewEmail();
        var after2 = ImportRequests.NewEmail();
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(before, "Dòng 2");
        workbook.AddValidRow(raced, "Dòng 3 — thua cuộc đua");
        workbook.AddValidRow(after1, "Dòng 4");
        workbook.AddValidRow(after2, "Dòng 5");

        var response = await ImportRequests.PostAsync(admin, workbook.ToBytes());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(race.Happened, "Giả lập đua email không chạy");
        var result = JsonSerializer.Deserialize<ImportResultDto>(
            await response.Content.ReadAsStringAsync(), JsonSerializerOptions.Web)!;
        Assert.Equal(new[] { "Created", "Skipped", "Created", "Created" }, result.Rows.Select(r => r.Status));
        Assert.Equal(new ImportCellErrorDto("Địa chỉ email", "email đã có hồ sơ"),
            Assert.Single(result.Rows.Single(r => r.Row == 3).Errors));
        Assert.Equal(3, result.Created);
        Assert.Equal(1, result.Skipped);
        Assert.True(await ImportRequests.StudentExistsAsync(factory, after1));
        Assert.True(await ImportRequests.StudentExistsAsync(factory, after2));
        Assert.False(await ImportRequests.StudentExistsAsync(factory, raced));
    }

    private sealed class RaceOnInsert
    {
        public const string Prefix = "race-";
        private readonly ConcurrentDictionary<string, bool> _raced = new(StringComparer.OrdinalIgnoreCase);

        public bool Happened => !_raced.IsEmpty;

        /// <summary>true đúng một lần cho mỗi email — lần chèn đối thủ không tự kích hoạt đua lần nữa.</summary>
        public bool TryClaim(string email) => email.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && _raced.TryAdd(email, true);
    }

    /// <summary>
    /// Ngay trước khi lưu một AppUser "race-…" (Identity đã kiểm trùng xong), một request khác — scope + kết nối riêng,
    /// commit ngay — tạo tài khoản cùng email. Lệnh INSERT của import vì vậy đụng chỉ mục duy nhất (2601).
    /// </summary>
    private sealed class RaceInterceptor(RaceOnInsert race, IServiceScopeFactory scopes) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var added = eventData.Context!.ChangeTracker.Entries<AppUser>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity.Email!)
                .ToList();
            foreach (var email in added.Where(race.TryClaim))
            {
                await using var scope = scopes.CreateAsyncScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var competitor = new AppUser { Id = Guid.NewGuid(), UserName = email, Email = email, FullName = "Request khác" };
                var created = await users.CreateAsync(competitor);
                Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            }
            return result;
        }
    }
}
