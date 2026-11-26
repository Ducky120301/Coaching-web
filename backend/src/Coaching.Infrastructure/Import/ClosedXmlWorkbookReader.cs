using System.Globalization;
using ClosedXML.Excel;
using Coaching.Application.Students.Import;

namespace Coaching.Infrastructure.Import;

/// <summary>
/// @ADR-f02-import-excel-004: đọc .xlsx bằng ClosedXML (MIT). File chép vào bộ nhớ (không ghi đĩa), chỉ đọc sheet đầu
/// (@BR-f02-import-excel-017), ô giữ kiểu chữ / số / ngày cho Task 5 (ngày, giá). File mở không được — .csv, .xls,
/// .xlsx hỏng hoặc có mật khẩu — đều là InvalidWorkbookException (@BR-f02-import-excel-014).
/// </summary>
internal sealed class ClosedXmlWorkbookReader : IWorkbookReader
{
    private static readonly WorkbookCell BlankCell = new(null, null, null);

    public WorkbookSheet Read(Stream xlsx)
    {
        ArgumentNullException.ThrowIfNull(xlsx);
        try
        {
            using var buffer = new MemoryStream();
            xlsx.CopyTo(buffer);
            buffer.Position = 0;
            using var workbook = new XLWorkbook(buffer);
            return ReadFirstSheet(workbook.Worksheet(1));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidWorkbookException($"Không đọc được file .xlsx ({ex.GetType().Name}).");
        }
    }

    private static WorkbookSheet ReadFirstSheet(IXLWorksheet sheet)
    {
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0; // dòng trống ở cuối không tính

        var headers = new List<string>(lastColumn);
        for (var c = 1; c <= lastColumn; c++)
            headers.Add(ToCell(sheet.Cell(1, c)).Text ?? string.Empty);

        var rows = new List<WorkbookRow>(Math.Max(0, lastRow - 1));
        for (var r = 2; r <= lastRow; r++)
        {
            var cells = new WorkbookCell[lastColumn];
            for (var c = 1; c <= lastColumn; c++)
                cells[c - 1] = ToCell(sheet.Cell(r, c));
            rows.Add(new WorkbookRow(r, cells));
        }
        return new WorkbookSheet(headers, rows);
    }

    private static WorkbookCell ToCell(IXLCell cell) => cell.DataType switch
    {
        XLDataType.Blank => BlankCell,
        XLDataType.Number => FromNumber(cell.GetDouble()),
        XLDataType.DateTime => FromDate(cell.GetDateTime()),
        XLDataType.Text => new WorkbookCell(TextOrNull(cell.GetText()), null, null),
        _ => new WorkbookCell(TextOrNull(cell.GetFormattedString()), null, null),
    };

    private static WorkbookCell FromNumber(double number) =>
        new(number.ToString(CultureInfo.InvariantCulture), number, null);

    private static WorkbookCell FromDate(DateTime date) =>
        new(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), null, date);

    private static string? TextOrNull(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
