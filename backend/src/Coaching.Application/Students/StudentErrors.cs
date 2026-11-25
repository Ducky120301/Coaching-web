using Coaching.Application.Common;

namespace Coaching.Application.Students;

public static class StudentErrors
{
    public static readonly Error NotFound =
        new("student.not_found", "Không tìm thấy hồ sơ học viên.", ErrorKind.NotFound);

    public static readonly Error CoachNotFound =
        new("coach.not_found", "Tài khoản này chưa có hồ sơ coach.", ErrorKind.NotFound);

    // @BR-f02-ho-so-hoc-vien-001
    public static readonly Error EmailTaken =
        new("student.email_taken", "Email này đã được dùng cho một tài khoản khác.", ErrorKind.Conflict);

    public static readonly Error VersionRequired =
        new("student.version_required", "Thiếu phiên bản hồ sơ. Tải lại hồ sơ rồi thử lại.");

    public static readonly Error RegistrationRequired =
        new("student.registration_required", "Thiếu nội dung form đăng ký.");

    // @BR-f02-ho-so-hoc-vien-015 @ADR-f02-ho-so-hoc-vien-004
    public static readonly Error VersionConflict =
        new("student.version_conflict", "Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.", ErrorKind.Conflict);

    public static readonly Error StatusInvalid =
        new("student.status_invalid", "Trạng thái coaching không hợp lệ.");
}
