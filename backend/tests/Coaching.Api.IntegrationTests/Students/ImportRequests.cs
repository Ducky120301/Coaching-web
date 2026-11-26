using System.Net.Http.Headers;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

internal static class ImportRequests
{
    public const string Url = "/api/admin/students/import";

    public static string NewEmail() => $"import-{Guid.NewGuid():N}@test.local";

    public static byte[] OneRow(string email)
    {
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(email, "Học viên Import");
        return workbook.ToBytes();
    }

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, byte[] file, string fileName = "cau-tra-loi.xlsx")
    {
        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue(ImportWorkbook.XlsxContentType);
        content.Add(part, "file", fileName);
        return await client.PostAsync(Url, content);
    }

    public static async Task<bool> StudentExistsAsync(ApiFactory factory, string email)
    {
        var value = Email.Create(email);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Students.AnyAsync(s => s.Email == value);
    }
}
