using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students.Import;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportResultTests(ApiFactory factory)
{
    // @BR-f02-import-excel-034: 5 dòng — 3 hợp lệ, 1 trùng email đã có, 1 tuổi sai → "Đã tạo 3, bỏ qua 2" kèm lý do theo số dòng Excel
    // @BR-f02-import-excel-023 @BR-f02-import-excel-025
    [Fact]
    public async Task Result_reports_counts_and_reason_per_excel_row()
    {
        var admin = await factory.AdminClientAsync();
        var existing = ImportRequests.NewEmail();
        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(StudentRequests.Url, new { fullName = "Đã có hồ sơ", email = existing })).StatusCode);
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(ImportRequests.NewEmail(), "Học viên 1");                 // dòng 2
        workbook.AddValidRow(existing.ToUpperInvariant(), "Trùng email đã có");        // dòng 3
        workbook.AddValidRow(ImportRequests.NewEmail(), "Học viên 2");                 // dòng 4
        var wrongAge = workbook.AddValidRow(ImportRequests.NewEmail(), "Tuổi sai");    // dòng 5
        workbook.Set(wrongAge, ImportWorkbook.AgeHeader, 5);
        workbook.AddValidRow(ImportRequests.NewEmail(), "Học viên 3");                 // dòng 6

        var response = await ImportRequests.PostAsync(admin, workbook.ToBytes());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ImportResultDto>(json, JsonSerializerOptions.Web)!;
        Assert.Equal(3, result.Created);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, result.Rows.Select(r => r.Row));

        var duplicate = result.Rows.Single(r => r.Row == 3);
        Assert.Equal("Skipped", duplicate.Status);
        Assert.Equal(existing, duplicate.Email);
        Assert.Equal(new ImportCellErrorDto("Địa chỉ email", "email đã có hồ sơ"), Assert.Single(duplicate.Errors));

        var age = result.Rows.Single(r => r.Row == 5);
        Assert.Equal("Skipped", age.Status);
        var ageError = Assert.Single(age.Errors);
        Assert.Equal("Tuổi (Age)", ageError.Column);
        Assert.Contains("phải từ 10 đến 100", ageError.Reason, StringComparison.Ordinal);

        Assert.All(result.Rows.Where(r => r.Status == "Created"), r =>
        {
            Assert.NotNull(r.StudentId);
            Assert.Empty(r.Errors);
        });

        // NFR-f02-import-excel-002: mỗi dòng kết quả chỉ có số dòng, email, trạng thái, Id hồ sơ và lỗi (cột + lý do)
        string[] allowed = ["row", "email", "status", "studentId", "errors"];
        using var document = JsonDocument.Parse(json);
        foreach (var row in document.RootElement.GetProperty("rows").EnumerateArray())
            Assert.All(row.EnumerateObject().Select(p => p.Name), name => Assert.Contains(name, allowed));
    }
}
