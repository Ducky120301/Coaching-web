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

    /// <summary>
    /// Chỉ duyệt ô có dữ liệu (RowsUsed / CellsUsed), không duyệt cả vùng dòng × cột — một ô lạc ở xa (vd XFD1048576)
    /// không làm đọc hàng tỉ ô. Số cột = cột cuối có tiêu đề ở dòng 1 (ô ngoài các cột đó không thuộc mục nào,
    /// @BR-f02-import-excel-020). Dòng trống bị bỏ (@BR-f02-import-excel-022). Dừng khi đã có MaxRows + 1 dòng dữ liệu —
    /// đủ để handler báo vượt giới hạn (@BR-f02-import-excel-016).
    /// </summary>
    private static WorkbookSheet ReadFirstSheet(IXLWorksheet sheet)
    {
        var headerRow = sheet.Row(1);
        var width = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        var headers = Fill(headerRow, width).Select(c => c.Text ?? string.Empty).ToList();

        var rows = new List<WorkbookRow>();
        foreach (var row in sheet.RowsUsed())
        {
            var rowNumber = row.RowNumber();
            if (rowNumber == 1)
                continue;
            var cells = Fill(row, width);
            if (cells.All(c => c.Text is null && c.Number is null && c.Date is null))
                continue;
            rows.Add(new WorkbookRow(rowNumber, cells));
            if (rows.Count > ImportStudentsHandler.MaxRows)
                break;
        }
        return new WorkbookSheet(headers, rows);
    }

    private static WorkbookCell[] Fill(IXLRow row, int width)
    {
        var cells = new WorkbookCell[width];
        Array.Fill(cells, BlankCell);
        foreach (var cell in row.CellsUsed())
        {
            var column = cell.Address.ColumnNumber;
            if (column <= width)
                cells[column - 1] = ToCell(cell);
        }
        return cells;
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
