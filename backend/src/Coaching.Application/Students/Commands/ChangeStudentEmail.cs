using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>@BR-f01-p1-tai-khoan-hoc-vien-007: email đi route riêng (UpdateStudentCommand vẫn không có email).</summary>
public sealed record ChangeStudentEmailCommand(string? Email, string? Version);

public sealed class ChangeStudentEmailHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IAccountCredentials credentials,
    CredentialsMailer mailer,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, ChangeStudentEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        // @BR-f01-p1-tai-khoan-hoc-vien-008: trim + lowercase; sai định dạng → 400 email.invalid
        var email = Email.Create(command.Email);

        // @BR-f01-p1-tai-khoan-hoc-vien-009: Identity + hồ sơ + mật khẩu tạm mới + thư trong một transaction — lỗi ở đâu cũng giữ email cũ
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            if (await students.GetForUpdateAsync(id, coach.Id, version, ct) is not { } student)
                return Result<StudentDetailDto>.Failure(StudentErrors.NotFound);
            if (student.AccountStatus == AccountStatus.Activated)
                return Result<StudentDetailDto>.Failure(AccountErrors.EmailLocked); // @BR-f01-p1-tai-khoan-hoc-vien-007 → 409

            var now = clock.UtcNow;
            if (student.Email == email)
                return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now)); // không đổi gì

            // @BR-f01-p1-tai-khoan-hoc-vien-008: kiểm trùng ở Identity (gồm tài khoản coach) TRƯỚC khi đổi hồ sơ
            var account = await credentials.ChangeEmailAsync(student.UserId, email, ct);
            if (!account.IsSuccess)
                return Result<StudentDetailDto>.Failure(account.Error!);

            var mustSend = student.ChangeEmail(email, coach.UserId, now);
            try
            {
                await unitOfWork.SaveChangesAsync(ct); // trước mọi lệnh UserManager tiếp theo → xung đột phiên bản thành 409
            }
            catch (ConcurrencyConflictException)
            {
                return Result<StudentDetailDto>.Failure(StudentErrors.VersionConflict);
            }

            // @BR-f01-p1-tai-khoan-hoc-vien-009: đã gửi → mật khẩu tạm mới (mật khẩu cũ + mã kích hoạt cũ bị hủy) tới email mới
            // @BR-f01-p1-tai-khoan-hoc-vien-010: chưa gửi lần nào → không gửi thư
            if (mustSend && await mailer.SendAsync(student, ct) is { IsSuccess: false } sent)
                return Result<StudentDetailDto>.Failure(sent.Error!);

            return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now));
        }, cancellationToken);
    }
}
