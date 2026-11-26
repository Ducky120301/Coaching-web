namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f02-import-excel-003: một mục sai của form đăng ký.
/// <paramref name="Code"/> = mã mục (vd "age"); <paramref name="Message"/> = lý do tiếng Việt KHÔNG kèm tên mục
/// (vd "phải từ 10 đến 100") — import ghép với tên cột trong file.
/// </summary>
public sealed record RegistrationFieldError(string Code, string Message);
