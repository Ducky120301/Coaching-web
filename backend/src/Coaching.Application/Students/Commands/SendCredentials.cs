using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Coach gửi / gửi lại thông tin đăng nhập (mật khẩu tạm 72 giờ) tới email của học viên.</summary>
public sealed class SendCredentialsHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    CredentialsMailer mailer,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @ADR-f01-p1-tai-khoan-hoc-vien-004: đổi hồ sơ + mật khẩu + gửi thư trong một transaction, thư là bước cuối trước commit.
        // Đọc hồ sơ BÊN TRONG operation: execution strategy chạy lại operation khi lỗi tạm thời.
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            if (await students.GetAsync(id, coach.Id, ct) is not { } student)
                return Result<StudentDetailDto>.Failure(StudentErrors.NotFound);
            // Kiểm trước để trả đúng mã: DomainException của aggregate luôn map ra 400
            if (student.AccountStatus == AccountStatus.Activated)
                return Result<StudentDetailDto>.Failure(AccountErrors.AlreadyActivated); // @BR-f01-p1-tai-khoan-hoc-vien-001 → 409
            if (student.CredentialsSentAt is null && student.MissingRequiredFields.Count > 0)
                return Result<StudentDetailDto>.Failure(
                    AccountErrors.RequiredFieldsMissing(student.MissingRequiredFields.Count)); // @BR-f01-p1-tai-khoan-hoc-vien-002

            var now = clock.UtcNow;
            student.RecordCredentialsSent(coach.UserId, now);
            try
            {
                // Lưu hồ sơ TRƯỚC khi UserManager tự SaveChanges → xung đột phiên bản đi qua ConcurrencyConflictException
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<StudentDetailDto>.Failure(StudentErrors.VersionConflict);
            }

            // @BR-f01-p1-tai-khoan-hoc-vien-006: gửi lỗi → trả lỗi → rollback: mật khẩu tạm cũ, hạn cũ, lịch sử giữ nguyên
            var sent = await mailer.SendAsync(student, ct);
            return sent.IsSuccess
                ? Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now)) // @BR-f01-p1-tai-khoan-hoc-vien-025: không có mật khẩu
                : Result<StudentDetailDto>.Failure(sent.Error!);
        }, cancellationToken);
    }
}
