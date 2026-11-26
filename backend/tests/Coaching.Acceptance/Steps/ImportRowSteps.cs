using System.Globalization;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Students.Import;
using Coaching.Domain.Students;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

/// <summary>import-excel.feature — mức dòng (BR-f02-import-excel-023…033).</summary>
[Binding]
public sealed class ImportRowSteps(ImportApi import, StudentApi api, AcceptanceContext ctx)
{
    /// <summary>"dòng đó" = dòng dữ liệu duy nhất của file (dòng Excel 2).</summary>
    private const int ThatRow = 2;

    /// <summary>Họ tên hồ sơ do step "email … đã thuộc về một hồ sơ học viên" tạo.</summary>
    private const string OwnerName = "Chủ email";

    // @BR-f02-import-excel-023
    [Given(@"^một file Excel có dòng 2 hợp lệ và dòng 3 có ""([^""]*)"" là ""([^""]*)""$")]
    public void Row2ValidRow3With(string header, string value)
    {
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        var row = import.Workbook.AddValidRow("minh@gmail.com", "Trần Văn Minh");
        import.Workbook.Set(row, header, value);
    }

    // @BR-f02-import-excel-023
    [Then(@"^chỉ học viên ở dòng (\d+) được tạo hồ sơ$")]
    public async Task OnlyRowCreated(int excelRow)
    {
        var created = Assert.Single(import.OkResult().Rows, r => r.Status == "Created");
        Assert.Equal(excelRow, created.Row);
        Assert.Equal(import.StudentsBefore + 1, await import.CountStudentsAsync());
    }

    // @BR-f02-import-excel-023: số dòng Excel + tên cột + lý do
    [Then(@"^kết quả import báo dòng (\d+), cột ""([^""]*)"" (.+)$")]
    public void RowColumnReason(int excelRow, string column, string reason) =>
        AssertCellError(import.Row(excelRow), column, reason);

    // @BR-f02-import-excel-023 @BR-f02-import-excel-030 @BR-f02-import-excel-031 @BR-f02-import-excel-032
    [Given(@"^một file Excel có 1 dòng học viên với ""([^""]*)"" là ""([^""]*)""$")]
    public void OneRowWith(string header, string value)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, header, value); // "Địa chỉ email" → thay email của dòng
    }

    // @BR-f02-import-excel-023
    [Then(@"^kết quả import báo dòng đó, cột ""([^""]*)"", lý do ""([^""]*)""$")]
    public void ThatRowColumnReason(string column, string reason) => AssertCellError(import.Row(ThatRow), column, reason);

    // @BR-f02-import-excel-023: "hôm nay" = 09:00 giờ Việt Nam của ngày đó
    [Given(@"^hôm nay là ""([^""]*)""$")]
    public Task TodayIs(string date) => api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));

    // @BR-f02-import-excel-023: một dòng có hai ô sai
    [Given(@"^một file Excel có 1 dòng học viên với ""([^""]*)"" là ""([^""]*)"" và ""([^""]*)"" là ""([^""]*)""$")]
    public void OneRowWithTwo(string firstHeader, string firstValue, string secondHeader, string secondValue)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, firstHeader, firstValue).Set(row, secondHeader, secondValue);
    }

    // @BR-f02-import-excel-023: báo cả hai ô
    [Then(@"^kết quả import báo dòng đó có (\d+) ô sai: (.+)$")]
    public void ThatRowHasErrors(int count, string columns)
    {
        var row = import.Row(ThatRow);
        Assert.Equal("Skipped", row.Status);
        Assert.Equal(count, row.Errors.Count);
        Assert.Equal(StepText.Quoted(columns).Order(), row.Errors.Select(e => e.Column).Order());
    }

    // @BR-f02-import-excel-024
    [Given("một file Excel có 1 dòng học viên có email nhưng để trống họ tên")]
    public void RowWithoutName() => import.Workbook.AddRow("lan@gmail.com", null);

    // @BR-f02-import-excel-024
    [Then(@"^kết quả import báo dòng đó thiếu ""([^""]*)""$")]
    public void ThatRowMissing(string column)
    {
        var row = import.Row(ThatRow);
        Assert.Equal("Skipped", row.Status);
        Assert.Contains(row.Errors, e => e.Column.StartsWith(column, StringComparison.Ordinal) && e.Reason == "bắt buộc");
    }

    // @BR-f02-import-excel-025: email có thể nằm trong ngoặc kép để giữ khoảng trắng đầu/cuối: "" quan@gmail.com ""
    [Given(@"^một file Excel có 1 dòng học viên với email ""(.*)""$")]
    public void OneRowWithEmail(string email) => import.Workbook.AddValidRow(Unquote(email), "Phạm Hoàng Quân");

    // @BR-f02-import-excel-025
    [Then("không hồ sơ mới nào được tạo")]
    public async Task NoNewProfile()
    {
        Assert.Equal(import.StudentsBefore, await import.CountStudentsAsync());
        Assert.Equal(0, import.OkResult().Created);
    }

    // @BR-f02-import-excel-025
    [Then("kết quả import báo dòng đó trùng email đã có")]
    public void ThatRowDuplicatesExisting() =>
        AssertCellError(import.Row(ThatRow), ImportWorkbook.EmailHeader, "email đã có hồ sơ");

    // @BR-f02-import-excel-025: hồ sơ / tài khoản có sẵn không bị thay đổi
    [Then(@"^thông tin của (một hồ sơ học viên|tài khoản coach) không thay đổi$")]
    public async Task OwnerUnchanged(string owner)
    {
        var usersNow = await import.UsersSnapshotAsync();
        Assert.All(import.UsersBefore, user => Assert.Contains(user, usersNow));
        if (owner == "một hồ sơ học viên")
            Assert.Equal(import.ProfilesBefore[OwnerName], JsonSerializer.Serialize(await api.GetAsync(OwnerName)));
    }

    // @BR-f02-import-excel-025
    [Given(@"^coach đã import thành công một file Excel có (\d+) dòng học viên$")]
    public async Task AlreadyImported(int count)
    {
        import.Workbook.AddValidRows(count);
        Assert.Equal(count, (await import.ImportOkAsync()).Created);
    }

    // @BR-f02-import-excel-025
    [When("coach import lại đúng file đó")]
    public Task ImportSameFileAgain() => import.ImportAsync(import.LastFile);

    // @BR-f02-import-excel-025
    [Then(@"^kết quả import báo cả (\d+) dòng trùng email đã có$")]
    public void AllRowsDuplicateExisting(int count)
    {
        var result = import.OkResult();
        Assert.Equal(count, result.Skipped);
        Assert.Equal(count, result.Rows.Count);
        Assert.All(result.Rows, r => AssertCellError(r, ImportWorkbook.EmailHeader, "email đã có hồ sơ"));
    }

    // @BR-f02-import-excel-026
    [Given(@"^một file Excel có dòng (\d+) và dòng (\d+) cùng email ""([^""]*)"", khác hoa/thường$")]
    public void SameEmailOnTwoRows(int first, int second, string email)
    {
        for (var row = 2; row <= second; row++)
        {
            var value = row == first ? email : row == second ? email.ToUpperInvariant() : $"hocvien{row}@gmail.com";
            import.Workbook.AddValidRow(value, $"Học viên dòng {row}");
        }
    }

    // @BR-f02-import-excel-026
    [Then(@"^chỉ một hồ sơ ""([^""]*)"" được tạo, theo dòng (\d+)$")]
    public async Task OnlyOneProfileFromRow(string email, int excelRow)
    {
        var row = import.Row(excelRow);
        Assert.Equal("Created", row.Status);
        var profile = Assert.Single(await api.ListAsync(null), s => s.Email == email);
        Assert.Equal(row.StudentId, profile.Id);
        Assert.Equal($"Học viên dòng {excelRow}", profile.FullName);
    }

    // @BR-f02-import-excel-026
    [Then(@"^kết quả import báo dòng (\d+) trùng email với dòng (\d+)$")]
    public void RowDuplicatesRow(int excelRow, int first) =>
        AssertCellError(import.Row(excelRow), ImportWorkbook.EmailHeader, $"trùng email với dòng {first}");

    // @BR-f02-import-excel-027
    [Given("một file Excel có 1 dòng học viên chỉ có email và họ tên")]
    public void OnlyEmailAndName()
    {
        import.Workbook = new ImportWorkbook(ImportWorkbook.EmailHeader, ImportWorkbook.FullNameHeader);
        import.Workbook.AddRow("lan@gmail.com", "Nguyễn Thị Lan");
    }

    // @BR-f02-import-excel-027
    [Then("hồ sơ đó cho biết còn thiếu các mục bắt buộc")]
    public async Task ProfileReportsMissingRequired()
    {
        var profile = await import.ProfileOfRowAsync(ThatRow);
        Assert.Equal(RegistrationFields.All.Where(f => f.Required).Select(f => f.Code), profile.MissingRequired);
    }

    // @BR-f02-import-excel-028
    [Then("hồ sơ vừa tạo có tài khoản Chờ kích hoạt và coaching Đang coaching")]
    public async Task PendingActivationAndActive()
    {
        var profile = await import.ProfileOfRowAsync(ThatRow);
        Assert.Equal(StudentApi.AccountStatuses["Chờ kích hoạt"], profile.AccountStatus);
        Assert.Equal(StudentApi.CoachingStatuses["Đang coaching"], profile.CoachingStatus);
    }

    // @BR-f02-import-excel-030: mục ghi nhận đúng giá trị chuẩn trong danh sách
    [Then(@"^hồ sơ vừa tạo ghi nhận ""([^""]*)"" là ""([^""]*)""$")]
    public async Task CreatedProfileRecords(string label, string expected)
    {
        var profile = await import.ProfileOfRowAsync(ThatRow);
        var code = RegistrationFields.All.Single(f => f.Label == label).Code;
        var node = JsonSerializer.SerializeToNode(profile.Registration, JsonSerializerOptions.Web)![code];
        Assert.Equal(expected, node?.GetValue<string>());
    }

    // @BR-f02-import-excel-030 @BR-f02-import-excel-031 @BR-f02-import-excel-032
    [Then(@"^kết quả import báo dòng đó, cột ""([^""]*)"" không hợp lệ$")]
    public void ThatRowColumnInvalid(string column)
    {
        var row = import.Row(ThatRow);
        Assert.Equal("Skipped", row.Status);
        Assert.Contains(row.Errors, e => e.Column == column && e.Reason.Length > 0);
    }

    // @BR-f02-import-excel-031: ô ngày của Excel
    [Given(@"^một file Excel có 1 dòng học viên với ngày chuyển khoản là ô ngày Excel (\d\d/\d\d/\d{4})$")]
    public async Task TransferDateCell(string date)
    {
        await EnsureTodayIsOnOrAfterAsync(date);
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.TransferDateHeader, StepText.ParseDate(date).ToDateTime(TimeOnly.MinValue));
    }

    // @BR-f02-import-excel-031: chữ dd/MM/yyyy
    [Given(@"^một file Excel có 1 dòng học viên với ngày chuyển khoản là chữ ""([^""]*)""$")]
    public async Task TransferDateText(string date)
    {
        await EnsureTodayIsOnOrAfterAsync(date);
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.TransferDateHeader, date);
    }

    // @BR-f02-import-excel-031
    [Then(@"^hồ sơ vừa tạo có ngày chuyển khoản ""([^""]*)""$")]
    public async Task CreatedProfileTransferDate(string date) =>
        Assert.Equal(StepText.ParseDate(date), (await import.ProfileOfRowAsync(ThatRow)).Registration.TransferDate);

    // @BR-f02-import-excel-032: ô số
    [Given(@"^một file Excel có 1 dòng học viên với giá là ô số (\d+)$")]
    public void PriceNumberCell(long price)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.PriceHeader, price);
    }

    // @BR-f02-import-excel-032: chữ
    [Given(@"^một file Excel có 1 dòng học viên với giá là chữ ""([^""]*)""$")]
    public void PriceText(string price)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.PriceHeader, price);
    }

    // @BR-f02-import-excel-032
    [Then(@"^hồ sơ vừa tạo có giá ([\d.]+) đồng$")]
    public async Task CreatedProfilePrice(string price) =>
        Assert.Equal(long.Parse(price.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture),
            (await import.ProfileOfRowAsync(ThatRow)).Registration.Price);

    // @BR-f02-import-excel-033: dòng lỗi (tuổi ngoài 10–100) rải giữa các dòng hợp lệ
    [Given(@"^một file Excel có (\d+) dòng học viên, trong đó (\d+) dòng hợp lệ$")]
    public void RowsSomeInvalid(int total, int valid)
    {
        var invalidCount = total - valid;
        var step = Math.Max(1, total / Math.Max(1, invalidCount));
        var invalid = 0;
        for (var i = 1; i <= total; i++)
        {
            var row = import.Workbook.AddValidRow($"hocvien{i}@gmail.com", $"Học viên {i}");
            if (invalid < invalidCount && i % step == 0)
            {
                import.Workbook.Set(row, ImportWorkbook.AgeHeader, "5");
                invalid++;
            }
        }
        Assert.Equal(invalidCount, invalid);
    }

    private static void AssertCellError(ImportRowDto row, string column, string reason)
    {
        Assert.Equal("Skipped", row.Status);
        Assert.Null(row.StudentId);
        Assert.Contains(row.Errors, e => e.Column == column && e.Reason.Contains(reason, StringComparison.Ordinal));
    }

    private static string Unquote(string text) =>
        text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? text[1..^1] : text;

    /// <summary>Đồng hồ scenario bắt đầu 01/11/2026 — tua tới ngày chuyển khoản nếu còn sớm hơn (ngày không được sau hôm nay).</summary>
    private async Task EnsureTodayIsOnOrAfterAsync(string date)
    {
        if (StepText.VietnamDate(ctx.Clock.GetUtcNow()) < StepText.ParseDate(date))
            await api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));
    }
}
