using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students;
using Coaching.Application.Students.Import;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Acceptance.Support;

/// <summary>Import Excel trong một scenario: file đang soạn, lần import gần nhất và số hồ sơ ngay trước lần đó.</summary>
public sealed class ImportApi(AcceptanceContext ctx, StudentApi api)
{
    public const string Url = "/api/admin/students/import";

    public ImportWorkbook Workbook { get; set; } = ImportWorkbook.Standard();

    /// <summary>File không dựng từ <see cref="Workbook"/> (.csv, .xls, hỏng, có mật khẩu, 6 MB); null = dùng Workbook.</summary>
    public byte[]? RawFile { get; set; }

    public string FileName { get; set; } = "cau-tra-loi.xlsx";

    /// <summary>Nội dung file của lần import gần nhất (để "import lại đúng file đó").</summary>
    public byte[]? LastFile { get; private set; }

    public HttpStatusCode? LastStatus { get; private set; }

    public int StudentsBefore { get; private set; }

    public ImportResultDto? Result { get; private set; }

    /// <summary>@BR-f02-import-excel-025: mọi tài khoản (kèm role) ngay trước lần import gần nhất.</summary>
    public IReadOnlyList<string> UsersBefore { get; private set; } = [];

    /// <summary>@BR-f02-import-excel-025: hồ sơ đã biết tên trong scenario (StudentApi.Ids), JSON ngay trước lần import gần nhất.</summary>
    public Dictionary<string, string> ProfilesBefore { get; } = new();

    public async Task<HttpResponseMessage> ImportAsync(byte[]? file = null)
    {
        file ??= RawFile ?? Workbook.ToBytes();
        StudentsBefore = await CountStudentsAsync();
        UsersBefore = await UsersSnapshotAsync();
        ProfilesBefore.Clear();
        foreach (var name in api.Ids.Keys.ToList())
            ProfilesBefore[name] = JsonSerializer.Serialize(await api.GetAsync(name));

        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeOf(FileName));
        content.Add(part, "file", FileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AdminToken);
        var response = await ctx.Client().SendAsync(request);

        ctx.LastResponse = response;
        LastFile = file;
        LastStatus = response.StatusCode;
        Result = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<ImportResultDto>()
            : null;
        return response;
    }

    public async Task<ImportResultDto> ImportOkAsync(byte[]? file = null)
    {
        await ImportAsync(file);
        return OkResult();
    }

    /// <summary>Kết quả của lần import gần nhất — lần đó phải trả 200.</summary>
    public ImportResultDto OkResult()
    {
        Assert.Equal(HttpStatusCode.OK, LastStatus);
        return Result!;
    }

    public ImportRowDto Row(int excelRow) => Assert.Single(OkResult().Rows, r => r.Row == excelRow);

    /// <summary>Hồ sơ đã tạo từ dòng Excel <paramref name="excelRow"/> (đọc qua GET hồ sơ của coach).</summary>
    public async Task<StudentDetailDto> ProfileOfRowAsync(int excelRow)
    {
        var row = Row(excelRow);
        Assert.Equal("Created", row.Status);
        var response = await ctx.GetAsync($"/api/admin/students/{row.StudentId}", api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
    }

    public async Task<int> CountStudentsAsync()
    {
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Students.CountAsync();
    }

    public async Task<bool> StudentExistsAsync(string email)
    {
        var value = Email.Create(email);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Students.AnyAsync(s => s.Email == value);
    }

    /// <summary>Mỗi tài khoản một dòng: Id, email, họ tên, băm mật khẩu, security stamp, các role.</summary>
    public async Task<IReadOnlyList<string>> UsersSnapshotAsync()
    {
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await db.Users.AsNoTracking().ToListAsync();
        var roles = await db.UserRoles.AsNoTracking().ToListAsync();
        return users
            .OrderBy(u => u.Id)
            .Select(u => $"{u.Id}|{u.Email}|{u.FullName}|{u.PasswordHash}|{u.SecurityStamp}|" +
                string.Join(",", roles.Where(r => r.UserId == u.Id).Select(r => r.RoleId).Order()))
            .ToList();
    }

    /// <summary>Lần import gần nhất bị từ chối cả file: 400, đúng mã lỗi, tiêu đề chứa <paramref name="titleContains"/>.</summary>
    public async Task AssertRejectedAsync(string code, string? titleContains = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, LastStatus);
        using var json = JsonDocument.Parse(await ctx.LastResponse!.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        if (titleContains is not null)
            Assert.Contains(titleContains, json.RootElement.GetProperty("title").GetString(), StringComparison.Ordinal);
    }

    private static string ContentTypeOf(string fileName) => Path.GetExtension(fileName) switch
    {
        ".csv" => "text/csv",
        ".xls" => "application/vnd.ms-excel",
        _ => ImportWorkbook.XlsxContentType,
    };
}
