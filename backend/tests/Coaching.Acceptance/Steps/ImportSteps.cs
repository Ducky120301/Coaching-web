using Coaching.Acceptance.Support;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

/// <summary>import-excel.feature — mức file (BR-f02-import-excel-013…022) + step dùng chung cho Task 5.</summary>
[Binding]
public sealed class ImportSteps(ImportApi import, StudentApi api)
{
    // @BR-f02-import-excel-013 (dùng lại ở BR-f02-import-excel-029)
    [Given(@"^một file Excel có 1 dòng học viên hợp lệ với email ""([^""]*)""$")]
    public void OneValidRowWithEmail(string email) => import.Workbook.AddValidRow(email, "Nguyễn Thị Lan");

    [When("coach import file đó")]
    public Task CoachImports() => import.ImportAsync();

    // @BR-f02-import-excel-013: hồ sơ import nằm trong danh sách học viên của coach đã import
    [Then(@"^danh sách học viên của coach có hồ sơ ""([^""]*)""$")]
    public async Task CoachListHas(string email) =>
        Assert.Contains(await api.ListAsync(null), s => s.Email == email);

    // @BR-f02-import-excel-014
    [Given(@"^một file (\.csv|Excel cũ \.xls|\.xlsx bị hỏng|\.xlsx có đặt mật khẩu)$")]
    public void FileOfKind(string kind)
    {
        (import.RawFile, import.FileName) = kind switch
        {
            ".csv" => (ImportWorkbook.Csv(), "cau-tra-loi.csv"),
            "Excel cũ .xls" => (ImportWorkbook.LegacyXls(), "cau-tra-loi.xls"),
            ".xlsx bị hỏng" => (ImportWorkbook.BrokenXlsx(), "cau-tra-loi.xlsx"),
            _ => (ImportWorkbook.EncryptedXlsx(), "cau-tra-loi.xlsx"),
        };
    }

    // @BR-f02-import-excel-014
    [Then("file bị từ chối vì không phải file Excel .xlsx đọc được")]
    public Task RejectedNotXlsx() => import.AssertRejectedAsync("import.file_invalid", ".xlsx");

    // @BR-f02-import-excel-014 @BR-f02-import-excel-015 @BR-f02-import-excel-016 @BR-f02-import-excel-019
    // @BR-f02-import-excel-021 (Task 5 dùng lại cho dòng bị bỏ qua)
    [Then("không hồ sơ nào được tạo")]
    public async Task NoProfileCreated()
    {
        Assert.Equal(import.StudentsBefore, await import.CountStudentsAsync());
        if (import.Result is { } result)
            Assert.Equal(0, result.Created);
    }

    // @BR-f02-import-excel-015
    [Given("một file Excel nặng 6 MB")]
    public void SixMegabyteFile() => import.RawFile = ImportWorkbook.OfSize(6);

    [Then("file bị từ chối vì vượt giới hạn 5 MB")]
    public Task RejectedTooLarge() => import.AssertRejectedAsync("import.file_too_large", "5 MB");

    // @BR-f02-import-excel-016 (dùng lại ở BR-f02-import-excel-028)
    [Given(@"^một file Excel có (\d+) dòng học viên hợp lệ$")]
    public void ValidRows(int count) => import.Workbook.AddValidRows(count);

    // @BR-f02-import-excel-016 @BR-f02-import-excel-019 @BR-f02-import-excel-020 @BR-f02-import-excel-022
    [Then(@"^(\d+) hồ sơ học viên mới được tạo$")]
    public async Task ProfilesCreated(int count)
    {
        Assert.Equal(count, import.OkResult().Created);
        Assert.Equal(import.StudentsBefore + count, await import.CountStudentsAsync());
    }

    // @BR-f02-import-excel-016
    [Given(@"^một file Excel có (\d+) dòng học viên$")]
    public void Rows(int count) => import.Workbook.AddValidRows(count);

    [Then("file bị từ chối vì vượt giới hạn 500 dòng")]
    public Task RejectedTooManyRows() => import.AssertRejectedAsync("import.too_many_rows", "500 dòng");

    // @BR-f02-import-excel-017
    [Given("một file Excel có 1 dòng học viên ở sheet đầu và 1 dòng học viên khác ở sheet thứ hai")]
    public void RowsOnTwoSheets()
    {
        import.Workbook.AddValidRow("sheetdau@gmail.com", "Học viên sheet đầu");
        import.Workbook.AddSecondSheetRow("sheethai@gmail.com", "Học viên sheet hai");
    }

    [Then("chỉ học viên ở sheet đầu được tạo hồ sơ")]
    public async Task OnlyFirstSheetCreated()
    {
        Assert.Equal(1, import.OkResult().Created);
        Assert.True(await import.StudentExistsAsync("sheetdau@gmail.com"));
        Assert.False(await import.StudentExistsAsync("sheethai@gmail.com"));
    }

    // @BR-f02-import-excel-018
    [Given(@"^một file Excel có cột ""([^""]*)"" đứng trước cột ""([^""]*)"" và 1 dòng học viên hợp lệ$")]
    public void ColumnBeforeColumn(string first, string second)
    {
        import.Workbook.MoveColumnBefore(first, second);
        Assert.True(import.Workbook.Headers.ToList().IndexOf(first) < import.Workbook.Headers.ToList().IndexOf(second));
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
    }

    [Then("hồ sơ được tạo với đúng họ tên và email của dòng đó")]
    public async Task CreatedWithNameAndEmailOfRow()
    {
        var profile = await import.ProfileOfRowAsync(2);
        Assert.Equal(import.Workbook.Get(2, ImportWorkbook.FullNameHeader), profile.FullName);
        Assert.Equal(import.Workbook.Get(2, ImportWorkbook.EmailHeader), profile.Email);
    }

    // @BR-f02-import-excel-019: thiếu cột không bắt buộc vẫn xử lý
    [Given(@"^một file Excel không có cột ""([^""]*)"" và 1 dòng học viên hợp lệ$")]
    public void WithoutColumnOneValidRow(string header)
    {
        import.Workbook.WithoutColumn(header);
        Assert.DoesNotContain(import.Workbook.Headers, h => h.StartsWith(header, StringComparison.Ordinal));
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
    }

    // @BR-f02-import-excel-019: thiếu cột email / họ tên → từ chối cả file
    [Given(@"^một file Excel không có cột ""([^""]*)""$")]
    public void WithoutColumn(string header)
    {
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.WithoutColumn(header);
    }

    [Then(@"^file bị từ chối vì thiếu cột ""([^""]*)""$")]
    public Task RejectedMissingColumn(string header) => import.AssertRejectedAsync("import.missing_column", header);

    // @BR-f02-import-excel-020
    [Given(@"^một file Excel có cột ""([^""]*)"" và 1 dòng học viên hợp lệ$")]
    public void ExtraColumnOneValidRow(string header)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, header, new DateTime(2026, 10, 30, 21, 54, 2)); // giá trị kiểu "Dấu thời gian" của Google Form
    }

    // @BR-f02-import-excel-020 @BR-f02-import-excel-022
    [Then("kết quả import không có lỗi nào")]
    public void NoErrors()
    {
        var result = import.OkResult();
        Assert.Equal(0, result.Skipped);
        Assert.All(result.Rows, r =>
        {
            Assert.Equal("Created", r.Status);
            Assert.Empty(r.Errors);
        });
    }

    // @BR-f02-import-excel-021
    [Given("một file Excel chỉ có dòng tiêu đề")]
    public void HeaderOnly() => Assert.Equal(1, import.Workbook.LastRow);

    [Then("coach được báo file không có dòng dữ liệu")]
    public Task RejectedNoRows() => import.AssertRejectedAsync("import.no_rows", "không có dòng dữ liệu");

    // @BR-f02-import-excel-022
    [Given("một file Excel có 2 dòng học viên hợp lệ và một dòng trống ở giữa")]
    public void TwoValidRowsBlankBetween()
    {
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.AddBlankRow();
        import.Workbook.AddValidRow("minh@gmail.com", "Trần Văn Minh");
    }
}
