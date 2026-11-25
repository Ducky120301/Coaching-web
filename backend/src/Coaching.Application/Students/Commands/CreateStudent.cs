using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Không có trường trạng thái: field lạ trong body bị bỏ qua (@BR-f02-ho-so-hoc-vien-005, @BR-f02-ho-so-hoc-vien-006).</summary>
public sealed record CreateStudentCommand(string? FullName, string? Email, RegistrationInput? Registration);

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
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coach.Id, Guid.CreateVersion7(), command.FullName, email, clock.UtcNow, registration);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ cùng một transaction
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
