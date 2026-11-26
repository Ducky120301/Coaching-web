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

    // @BR-f02-import-excel-022: dòng trống (ở giữa hay ở cuối) bị bỏ, số dòng Excel của dòng sau giữ nguyên
    [Fact]
    public void Skips_blank_rows_and_keeps_excel_row_numbers()
    {
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow("a@gmail.com", "A");
        workbook.AddBlankRow();
        workbook.AddValidRow("b@gmail.com", "B");
        workbook.AddBlankRow();

        var sheet = _reader.Read(new MemoryStream(workbook.ToBytes()));

        Assert.Equal(new[] { 2, 4 }, sheet.Rows.Select(r => r.RowNumber));
    }

    // @BR-f02-import-excel-016: một ô lạc ở tận XFD1048576 không làm đọc cả vùng dòng × cột — đọc theo dòng có dữ liệu,
    // ô ngoài các cột tiêu đề bỏ qua, và dừng khi đã có MaxRows + 1 dòng dữ liệu (đủ để báo vượt 500 dòng)
    [Fact]
    public void Stray_far_cell_does_not_expand_the_scan_and_reading_stops_after_max_rows_plus_one()
    {
        var workbook = ImportWorkbook.Standard().AddValidRows(ImportStudentsHandler.MaxRows + 20);
        var bytes = WithStrayCell(workbook.ToBytes(), "XFD1048576");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var sheet = _reader.Read(new MemoryStream(bytes));
        watch.Stop();

        Assert.Equal(ImportStudentsHandler.MaxRows + 1, sheet.Rows.Count);
        Assert.Equal(4, sheet.Headers.Count);
        Assert.All(sheet.Rows, r => Assert.Equal(4, r.Cells.Count));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"Đọc mất {watch.Elapsed}");
    }

    private static byte[] WithStrayCell(byte[] xlsx, string address)
    {
        using var input = new MemoryStream(xlsx);
        using var workbook = new ClosedXML.Excel.XLWorkbook(input);
        workbook.Worksheet(1).Cell(address).Value = "ô lạc";
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
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

    // @BR-f02-import-excel-014 @ADR-f02-import-excel-007: file nén cao ("zip bomb") — tổng dung lượng giải nén vượt
    // MaxUncompressedBytes → từ chối trước khi ClosedXML nạp workbook vào bộ nhớ
    [Fact]
    public void Workbook_whose_entries_inflate_beyond_the_cap_is_rejected()
    {
        var bytes = WithExtraEntry(ImportWorkbook.Standard().AddValidRows(3).ToBytes(), "xl/worksheets/sheet2.xml",
            ClosedXmlWorkbookReader.MaxUncompressedBytes + 1);

        Assert.True(bytes.Length < 1024 * 1024, $"File test phải nhỏ (nén cao): {bytes.Length} byte");
        Assert.Throws<InvalidWorkbookException>(() => _reader.Read(new MemoryStream(bytes)));
    }

    // @BR-f02-import-excel-014 @ADR-f02-import-excel-007: một entry có tỉ lệ nén phi lý (dưới mức tổng nhưng vài KB
    // nở thành hàng chục MB) cũng bị từ chối
    [Fact]
    public void Workbook_with_an_absurdly_compressed_entry_is_rejected()
    {
        var bytes = WithExtraEntry(ImportWorkbook.Standard().AddValidRows(3).ToBytes(), "xl/worksheets/sheet2.xml",
            20 * 1024 * 1024);

        Assert.Throws<InvalidWorkbookException>(() => _reader.Read(new MemoryStream(bytes)));
    }

    /// <summary>Chép file .xlsx và thêm một entry toàn khoảng trắng (nén ~1000:1), ghi theo khối để test không tốn RAM.</summary>
    private static byte[] WithExtraEntry(byte[] xlsx, string name, long size)
    {
        var output = new MemoryStream();
        output.Write(xlsx);
        using (var zip = new System.IO.Compression.ZipArchive(output, System.IO.Compression.ZipArchiveMode.Update, leaveOpen: true))
        {
            using var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.SmallestSize).Open();
            var chunk = new byte[1024 * 1024];
            Array.Fill(chunk, (byte)' ');
            for (var written = 0L; written < size; written += chunk.Length)
                entry.Write(chunk, 0, (int)Math.Min(chunk.Length, size - written));
        }
        return output.ToArray();
    }
}
