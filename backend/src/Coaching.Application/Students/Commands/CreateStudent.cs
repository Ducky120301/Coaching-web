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
    StudentRegistrar registrar,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var email = Email.Create(command.Email); // @BR-f02-ho-so-hoc-vien-002 → 400 email.required / email.invalid
        // @ADR-f02-ho-so-hoc-vien-006: luật form ở Domain; @BR-f02-ho-so-hoc-vien-010: "hôm nay" theo giờ VN
        var registration = RegistrationForm.Create(command.Registration ?? new RegistrationInput(), clock.Today);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ (kèm ghi chú) cùng một transaction
        var registered = await registrar.RegisterAsync(coach.Id, command.FullName, email, registration,
            command.InternalNote, cancellationToken);
        if (!registered.IsSuccess)
            return registered.Error!; // @BR-f02-ho-so-hoc-vien-001 → 409

        var student = registered.Value;
        return StudentDetailDto.From(student, coach, student.CreatedAt);
    }
}
