namespace Coaching.Application.Abstractions;

/// <summary>
/// Ghi vết thao tác quan trọng (BR-42): phát hành, đổi trạng thái phase, sửa chỉ tiêu,
/// gửi phản hồi, đổi trạng thái coaching. Người thực hiện lấy từ <see cref="ICurrentUser"/>.
/// Bản ghi được thêm vào unit of work hiện tại và lưu cùng thay đổi nghiệp vụ.
/// </summary>
public interface IAuditLog
{
    void Record(string action, string entityType, string entityId, string? details = null);
}
