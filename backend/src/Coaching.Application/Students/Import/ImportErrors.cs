using Coaching.Application.Common;

namespace Coaching.Application.Students.Import;

/// <summary>Lỗi cả file — từ chối cả file, không tạo hồ sơ nào (400).</summary>
public static class ImportErrors
{
    // @BR-f02-import-excel-014
    public static readonly Error FileInvalid = new("import.file_invalid", "File phải là Excel .xlsx đọc được.");

    // @BR-f02-import-excel-015
    public static readonly Error FileTooLarge = new("import.file_too_large", "File vượt giới hạn 5 MB.");

    // @BR-f02-import-excel-016
    public static readonly Error TooManyRows = new("import.too_many_rows", "File vượt giới hạn 500 dòng.");

    // @BR-f02-import-excel-021
    public static readonly Error NoRows = new("import.no_rows", "File không có dòng dữ liệu.");

    // @BR-f02-import-excel-019
    public static Error MissingColumn(string header) => new("import.missing_column", $"Thiếu cột \"{header}\".");
}
