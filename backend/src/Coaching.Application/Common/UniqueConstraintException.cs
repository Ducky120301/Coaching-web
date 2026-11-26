namespace Coaching.Application.Common;

/// <summary>
/// Lần lưu vi phạm chỉ mục duy nhất: hai request đua nhau qua bước kiểm trùng của use case. IUnitOfWork ném lỗi này để
/// use case trả lỗi nghiệp vụ (409) mà không phụ thuộc EF/SqlClient.
/// </summary>
public sealed class UniqueConstraintException(Exception innerException)
    : Exception("Dữ liệu trùng với một bản ghi đã có.", innerException);
