namespace Coaching.Application.Common;

/// <summary>@ADR-f02-ho-so-hoc-vien-004: dữ liệu đã được lưu ở nơi khác kể từ phiên bản request gửi lên.</summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("Dữ liệu đã được sửa ở nơi khác.", innerException);
