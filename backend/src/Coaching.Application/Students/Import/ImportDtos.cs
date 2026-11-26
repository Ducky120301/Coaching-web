namespace Coaching.Application.Students.Import;

/// <summary>Column = tiêu đề cột như trong file (dòng đầu); Reason = lý do tiếng Việt.</summary>
public sealed record ImportCellErrorDto(string Column, string Reason);

/// <summary>Một dòng có dữ liệu. Status "Created" | "Skipped"; StudentId chỉ có khi Created.</summary>
public sealed record ImportRowDto(int Row, string? Email, string Status, Guid? StudentId, IReadOnlyList<ImportCellErrorDto> Errors);

public sealed record ImportResultDto(int Created, int Skipped, IReadOnlyList<ImportRowDto> Rows);
