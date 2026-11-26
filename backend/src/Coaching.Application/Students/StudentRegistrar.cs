using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>
/// Tạo hồ sơ học viên + tài khoản đăng nhập Chờ kích hoạt — dùng chung cho tạo hồ sơ (F02) và import Excel.
/// Không gửi email. Form đã kiểm trước (RegistrationForm); họ tên/ghi chú sai → DomainException TRƯỚC khi tạo tài khoản.
/// </summary>
public sealed class StudentRegistrar(
    IStudentRepository students,
    IStudentAccounts accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<Student>> RegisterAsync(Guid coachId, string? fullName, Email email,
        RegistrationForm registration, string? internalNote, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007: Chờ kích hoạt, Đang coaching, thuộc coach.
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coachId, Guid.CreateVersion7(), fullName, email, now, registration);
        // @BR-f02-ho-so-hoc-vien-014: ghi chú lúc tạo có thời điểm sửa ghi chú = thời điểm tạo; quá 5000 ký tự → 400 trước khi tạo tài khoản
        student.UpdateInternalNote(internalNote, now);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ (kèm ghi chú) cùng một transaction
        return await unitOfWork.ExecuteInTransactionAsync<Student>(async token =>
        {
            var account = await accounts.CreatePendingAsync(student.UserId, email, student.FullName, token);
            if (!account.IsSuccess)
                return Result<Student>.Failure(account.Error!); // @BR-f02-ho-so-hoc-vien-001 → 409

            students.Add(student);
            await unitOfWork.SaveChangesAsync(token);
            return Result<Student>.Success(student);
        }, ct);
    }
}
