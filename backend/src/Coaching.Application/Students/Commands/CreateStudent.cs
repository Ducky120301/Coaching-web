using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>
/// Không có trường trạng thái: field lạ trong body bị bỏ qua (@BR-f02-ho-so-hoc-vien-005, @BR-f02-ho-so-hoc-vien-006).
/// <paramref name="InternalNote"/> tùy chọn — lưu cùng transaction tạo hồ sơ (@BR-f02-ho-so-hoc-vien-014).
/// </summary>
public sealed record CreateStudentCommand(
    string? FullName, string? Email, RegistrationInput? Registration, string? InternalNote = null);

public sealed class CreateStudentHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IStudentAccounts accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var email = Email.Create(command.Email); // @BR-f02-ho-so-hoc-vien-002 → 400 email.required / email.invalid
        // @ADR-f02-ho-so-hoc-vien-006: luật form ở Domain; @BR-f02-ho-so-hoc-vien-010: "hôm nay" theo giờ VN
        var registration = RegistrationForm.Create(command.Registration ?? new RegistrationInput(), clock.Today);
        var now = clock.UtcNow;
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coach.Id, Guid.CreateVersion7(), command.FullName, email, now, registration);
        // @BR-f02-ho-so-hoc-vien-014: ghi chú lúc tạo có thời điểm sửa ghi chú = thời điểm tạo; quá 5000 ký tự → 400 trước khi tạo tài khoản
        student.UpdateInternalNote(command.InternalNote, now);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ (kèm ghi chú) cùng một transaction
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            var account = await accounts.CreatePendingAsync(student.UserId, email, student.FullName, ct);
            if (!account.IsSuccess)
                return Result<StudentDetailDto>.Failure(account.Error!); // @BR-f02-ho-so-hoc-vien-001 → 409

            students.Add(student);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
        }, cancellationToken);
    }
}
