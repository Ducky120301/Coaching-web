using Coaching.Application.Students.Import;
using Coaching.Infrastructure.Import;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Infrastructure;

public sealed class ClosedXmlWorkbookReaderTests
{
    private readonly ClosedXmlWorkbookReader _reader = new();

    // @ADR-f02-import-excel-004 @BR-f02-import-excel-017: chỉ sheet đầu; ô giữ kiểu chữ / số / ngày
    [Fact]
    public void Reads_first_sheet_only_with_typed_cells()
    {
        var workbook = new ImportWorkbook(ImportWorkbook.EmailHeader, ImportWorkbook.FullNameHeader,
            ImportWorkbook.AgeHeader, ImportWorkbook.TransferDateHeader);
        var row = workbook.AddRow("  lan@gmail.com ", "Nguyễn Thị Lan");
        workbook.Set(row, ImportWorkbook.AgeHeader, 28).Set(row, ImportWorkbook.TransferDateHeader, new DateTime(2026, 11, 12));
        workbook.AddSecondSheetRow("khac@gmail.com", "Học viên sheet hai");

        var sheet = _reader.Read(new MemoryStream(workbook.ToBytes()));

        Assert.Equal(new[] { "Địa chỉ email", "Họ Và Tên (Name)", "Tuổi (Age)", "Ngày chuyển khoản" }, sheet.Headers);
        var only = Assert.Single(sheet.Rows);
        Assert.Equal(2, only.RowNumber);
        Assert.Equal("lan@gmail.com", only.Cells[0].Text);
        Assert.Null(only.Cells[0].Number);
        Assert.Equal(28d, only.Cells[2].Number);
        Assert.Equal("28", only.Cells[2].Text);
        Assert.Equal(new DateTime(2026, 11, 12), only.Cells[3].Date);
        Assert.Equal("12/11/2026", only.Cells[3].Text);
    }

    // @BR-f02-import-excel-022: dòng trống ở giữa vẫn trả về (handler bỏ qua), dòng trống ở cuối bị bỏ
    [Fact]
    public void Keeps_blank_middle_row_and_drops_trailing_blank_rows()
    {
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow("a@gmail.com", "A");
        workbook.AddBlankRow();
        workbook.AddValidRow("b@gmail.com", "B");
        workbook.AddBlankRow();

        var sheet = _reader.Read(new MemoryStream(workbook.ToBytes()));

        Assert.Equal(new[] { 2, 3, 4 }, sheet.Rows.Select(r => r.RowNumber));
        Assert.All(sheet.Rows[1].Cells, c => Assert.True(c.Text is null && c.Number is null && c.Date is null));
    }

    public static TheoryData<string> UnreadableKinds => new() { "csv", "xls", "broken", "encrypted" };

    // @BR-f02-import-excel-014 @ADR-f02-import-excel-004: .csv, .xls, .xlsx hỏng, .xlsx có mật khẩu → InvalidWorkbookException
    [Theory]
    [MemberData(nameof(UnreadableKinds))]
    public void Unreadable_file_throws_InvalidWorkbookException(string kind)
    {
        var bytes = kind switch
        {
            "csv" => ImportWorkbook.Csv(),
            "xls" => ImportWorkbook.LegacyXls(),
            "broken" => ImportWorkbook.BrokenXlsx(),
            _ => ImportWorkbook.EncryptedXlsx(),
        };

        var error = Record.Exception(() => _reader.Read(new MemoryStream(bytes)));

        Assert.True(error is InvalidWorkbookException, $"{kind}: {error?.GetType().Name ?? "không ném lỗi"}");
    }
}
