namespace Coaching.Application.Students.Import;

/// <summary>
/// @ADR-f02-import-excel-004: một ô đã đọc. Text = giá trị hiển thị đã trim (null nếu trống; ô số → số dạng invariant,
/// ô ngày → dd/MM/yyyy); Number chỉ có ở ô số; Date chỉ có ở ô ngày của Excel.
/// </summary>
public sealed record WorkbookCell(string? Text, double? Number, DateTime? Date);

/// <summary>RowNumber = số dòng Excel (tiêu đề = 1).</summary>
public sealed record WorkbookRow(int RowNumber, IReadOnlyList<WorkbookCell> Cells);

/// <summary>
/// Sheet đầu tiên: tiêu đề dòng 1 + các dòng dữ liệu (dòng trống đã bị bỏ). Reader được dừng sau
/// ImportStudentsHandler.MaxRows + 1 dòng dữ liệu — đủ để báo vượt giới hạn.
/// </summary>
public sealed record WorkbookSheet(IReadOnlyList<string> Headers, IReadOnlyList<WorkbookRow> Rows);

/// <summary>@ADR-f02-import-excel-004: port đọc .xlsx — Application không phụ thuộc thư viện Excel.</summary>
public interface IWorkbookReader
{
    /// <exception cref="InvalidWorkbookException">Không phải .xlsx, file hỏng hoặc có mật khẩu.</exception>
    WorkbookSheet Read(Stream xlsx);
}

public sealed class InvalidWorkbookException(string message) : Exception(message);
