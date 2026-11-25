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
}
