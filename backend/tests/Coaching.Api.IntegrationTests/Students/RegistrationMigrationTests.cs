using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

/// <summary>
/// @ADR-f02-import-excel-002: changeset Liquibase v0.4.0 chuyển `Students.Registration` cũ sang mục Google Form.
/// Template DB chạy Liquibase khi bảng Students còn trống → test seed JSON cũ (khoá = tên property C#, như EF ghi)
/// rồi chạy lại đúng file script của changeset.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RegistrationMigrationTests(ApiFactory factory)
{
    private const string ChangeSetId = "v0.4.0-0001-registration-google-form";

    private static readonly DateTimeOffset SeededNoteAt = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string OldKeys =
        "N'Availability', N'Equipment', N'BodyFatPercent', N'BodyFatMethod', N'WeightBeforeLiftingKg', " +
        "N'HeightThenCm', N'PhotoFrontUrl', N'PhotoBackUrl', N'PhotoLeftUrl', N'PhotoRightUrl'";

    // @ADR-f02-import-excel-002: Liquibase đã chạy changeset v0.4.0 khi dựng template DB
    [Fact]
    public async Task Changeset_v0_4_0_is_applied_by_liquibase() =>
        Assert.Equal(1, await ScalarAsync<int>("SELECT COUNT(*) FROM [DATABASECHANGELOG] WHERE [ID] = @id",
            new SqlParameter("@id", ChangeSetId)));

    // @ADR-f02-import-excel-002: tiền đề của script — EF ghi khoá JSON bằng tên property C# (PascalCase)
    [Fact]
    public async Task Ef_writes_registration_json_keys_as_property_names()
    {
        var admin = await factory.AdminClientAsync();
        var response = await admin.PostAsJsonAsync(StudentRequests.Url, new
        {
            fullName = "Lan",
            email = StudentRequests.NewEmail(),
            registration = new { bodyPhotoUrl = "https://x.vn/new" },
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;

        var raw = await ScalarAsync<string>("SELECT [Registration] FROM [Students] WHERE [Id] = @id", new SqlParameter("@id", id));

        Assert.Contains("\"BodyPhotoUrl\":", raw);
    }

    // @BR-f02-import-excel-002 @BR-f02-import-excel-008 @BR-f02-import-excel-009 @BR-f02-import-excel-010 @BR-f02-import-excel-011
    [Fact]
    public async Task Old_registration_is_moved_to_google_form_fields()
    {
        var admin = await factory.AdminClientAsync();
        var full = await SeedOldAsync(admin, """
            {"Pack":"Pack 2","TrainingYears":"1–2 năm","BodyFatPercent":18,"BodyFatMethod":"InBody",
             "WeightBeforeLiftingKg":60,"HeightThenCm":170,"Availability":"Tối 2-4-6","Equipment":"Tạ đơn",
             "PhotoFrontUrl":"https://x.vn/front","PhotoBackUrl":"https://x.vn/back",
             "PhotoLeftUrl":"https://x.vn/left","PhotoRightUrl":"https://x.vn/right"}
            """, note: "Hay đau gối", noteAt: SeededNoteAt);
        var partial = await SeedOldAsync(admin, """
            {"BodyFatPercent":18.5,"WeightBeforeLiftingKg":72.5,"PhotoLeftUrl":"https://x.vn/left"}
            """);
        var methodOnly = await SeedOldAsync(admin, """
            {"BodyFatMethod":"InBody","HeightThenCm":170,"PhotoFrontUrl":"https://x.vn/front","Availability":null}
            """, note: "Ghi chú cũ", noteAt: SeededNoteAt);
        var emptyOld = await SeedOldAsync(admin, """
            {"Pack":null,"BodyFatPercent":null,"BodyFatMethod":null,"WeightBeforeLiftingKg":null,"HeightThenCm":null,
             "Availability":null,"Equipment":null,"PhotoFrontUrl":null,"PhotoBackUrl":null,"PhotoLeftUrl":null,"PhotoRightUrl":null}
            """);

        await RunScriptAsync();

        // Có đủ dữ liệu cũ
        var a = await DetailAsync(admin, full);
        Assert.Equal("18 % – InBody", a.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Equal("60 kg, 170 cm", a.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Equal("https://x.vn/front", a.Registration.BodyPhotoUrl); // @BR-f02-import-excel-010: lấy link ảnh trước
        Assert.Equal("1–2 năm", a.Registration.TrainingYears); // @BR-f02-import-excel-003: hồ sơ cũ giữ nguyên
        Assert.Equal("Pack 2", a.Registration.Pack);
        Assert.Equal( // @BR-f02-import-excel-011: link thừa nối sau ghi chú đang có, mỗi link một dòng
            "Hay đau gối\nẢnh mặt sau: https://x.vn/back\nẢnh bên trái: https://x.vn/left\nẢnh bên phải: https://x.vn/right",
            a.InternalNote);
        Assert.NotNull(a.InternalNoteUpdatedAt);
        Assert.NotEqual(SeededNoteAt, a.InternalNoteUpdatedAt);
        Assert.Equal(0, await OldKeysLeftAsync(full)); // @BR-f02-import-excel-002: Lịch rảnh, Dụng cụ (và khoá đã gộp) bị xoá

        // Chỉ có mỡ, chỉ có cân nặng, chỉ có link ảnh trái; số thập phân kiểu VN
        var b = await DetailAsync(admin, partial);
        Assert.Equal("18,5 %", b.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Equal("72,5 kg", b.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Equal("https://x.vn/left", b.Registration.BodyPhotoUrl); // @BR-f02-import-excel-010
        Assert.Null(b.InternalNote); // @BR-f02-import-excel-011: 1 link → ghi chú không đổi
        Assert.Null(b.InternalNoteUpdatedAt);

        // Chỉ có cách đo, chỉ có chiều cao, 1 link
        var c = await DetailAsync(admin, methodOnly);
        Assert.Equal("InBody", c.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Equal("170 cm", c.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Equal("https://x.vn/front", c.Registration.BodyPhotoUrl);
        Assert.Equal("Ghi chú cũ", c.InternalNote); // @BR-f02-import-excel-011
        Assert.Equal(SeededNoteAt, c.InternalNoteUpdatedAt);
        Assert.Equal(0, await OldKeysLeftAsync(methodOnly));

        // Khoá cũ có nhưng đều null (EF ghi null) → mục gộp để trống, khoá cũ bị xoá
        var d = await DetailAsync(admin, emptyOld);
        Assert.Null(d.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Null(d.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Null(d.Registration.BodyPhotoUrl); // @BR-f02-import-excel-010
        Assert.Null(d.InternalNote);
        Assert.Equal(0, await OldKeysLeftAsync(emptyOld));
    }

    // @NFR-f02-import-excel-004 @ADR-f02-import-excel-002: chạy lại không đổi dữ liệu đã chuyển; hồ sơ dạng mới không bị chạm
    [Fact]
    public async Task Running_the_script_again_changes_nothing()
    {
        var admin = await factory.AdminClientAsync();
        var old = await SeedOldAsync(admin, """
            {"BodyFatPercent":18,"BodyFatMethod":"InBody","WeightBeforeLiftingKg":60,"HeightThenCm":170,
             "Availability":"Tối 2-4-6","PhotoFrontUrl":"https://x.vn/front","PhotoBackUrl":"https://x.vn/back"}
            """, note: "Hay đau gối", noteAt: SeededNoteAt);
        var created = await admin.PostAsJsonAsync(StudentRequests.Url, new
        {
            fullName = "Học viên dạng mới",
            email = StudentRequests.NewEmail(),
            registration = new { bodyPhotoUrl = "https://x.vn/new", bodyFatAndMethod = "20" },
            internalNote = "Ghi chú mới",
        });
        var fresh = (await created.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;
        var freshBefore = await SnapshotAsync(fresh);

        await RunScriptAsync();
        var oldAfterFirst = await SnapshotAsync(old);
        await RunScriptAsync();
        await RunScriptAsync();

        Assert.Equal(oldAfterFirst, await SnapshotAsync(old));
        Assert.Equal(freshBefore, await SnapshotAsync(fresh));
        var detail = await DetailAsync(admin, old);
        Assert.Equal("Hay đau gối\nẢnh mặt sau: https://x.vn/back", detail.InternalNote);
        Assert.Equal("18 % – InBody", detail.Registration.BodyFatAndMethod);
    }

    private async Task<Guid> SeedOldAsync(HttpClient admin, string json, string? note = null, DateTimeOffset? noteAt = null)
    {
        var created = await StudentRequests.CreateAsync(admin);
        await ExecAsync(
            "UPDATE [Students] SET [Registration] = @json, [InternalNote] = @note, [InternalNoteUpdatedAt] = @noteAt WHERE [Id] = @id",
            new SqlParameter("@json", json),
            new SqlParameter("@note", (object?)note ?? DBNull.Value),
            new SqlParameter("@noteAt", (object?)noteAt ?? DBNull.Value),
            new SqlParameter("@id", created.Id));
        return created.Id;
    }

    private static async Task<StudentDetailDto> DetailAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{id}"))!;

    private Task<int> OldKeysLeftAsync(Guid id) => ScalarAsync<int>(
        "SELECT COUNT(*) FROM [Students] AS s CROSS APPLY OPENJSON(s.[Registration]) AS j " +
        $"WHERE s.[Id] = @id AND j.[key] IN ({OldKeys})",
        new SqlParameter("@id", id));

    private Task<string> SnapshotAsync(Guid id) => ScalarAsync<string>(
        "SELECT CONCAT([Registration], N'|', [InternalNote], N'|', CONVERT(nvarchar(40), [InternalNoteUpdatedAt], 127), " +
        "N'|', CONVERT(nvarchar(20), CAST([Version] AS bigint))) FROM [Students] WHERE [Id] = @id",
        new SqlParameter("@id", id));

    /// <summary>Chạy đúng file script của changeset, tách batch theo dòng `GO` như Liquibase (endDelimiter "\nGO").</summary>
    private async Task RunScriptAsync()
    {
        var path = Path.Combine(DatabaseFixture.FindChangelogDirectory(),
            "v0.4.0", "scripts", "0001-registration-google-form.sql");
        var sql = await File.ReadAllTextAsync(path);
        foreach (var batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline).Where(b => !string.IsNullOrWhiteSpace(b)))
            await ExecAsync(batch);
    }

    private string ConnectionString()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()!;
    }

    private async Task ExecAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        command.Parameters.AddRange(parameters);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
