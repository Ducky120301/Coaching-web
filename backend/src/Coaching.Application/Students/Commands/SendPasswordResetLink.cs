using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Coach gửi link đặt lại mật khẩu (30 phút, dùng một lần) tới email của học viên đã kích hoạt.</summary>
public sealed class SendPasswordResetLinkHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IAccountTokens tokens,
    IAccountEmails emails,
    IEmailSender sender,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        // Gửi lỗi → rollback: không có link mới, lịch sử không thêm dòng (cùng mẫu ADR-f01-p1-tai-khoan-hoc-vien-004 của Task 4)
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            if (await students.GetAsync(id, coach.Id, ct) is not { } student)
                return Result<StudentDetailDto>.Failure(StudentErrors.NotFound);
            if (student.AccountStatus != AccountStatus.Activated)
                return Result<StudentDetailDto>.Failure(AccountErrors.NotActivated); // @BR-f01-p1-tai-khoan-hoc-vien-017 → 409

            var now = clock.UtcNow;
            student.RecordResetLinkSent(coach.UserId, now);
            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<StudentDetailDto>.Failure(StudentErrors.VersionConflict);
            }

            // @BR-f01-p1-tai-khoan-hoc-vien-018: 30 phút; @BR-f01-p1-tai-khoan-hoc-vien-020: link mới hủy mọi link cũ (IssueAsync)
            var rawToken = await tokens.IssueAsync(student.UserId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach,
                now + AccountTokenLifetimes.PasswordReset, ct);
            var sent = await AccountMail.SendAsync(sender, emails.PasswordReset(student.Email.Value, student.FullName, rawToken), ct);
            return sent.IsSuccess
                ? Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now))
                : Result<StudentDetailDto>.Failure(sent.Error!);
        }, cancellationToken);
    }
}
