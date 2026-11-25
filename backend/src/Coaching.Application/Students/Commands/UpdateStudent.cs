using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>@BR-f02-ho-so-hoc-vien-012: không có Email — email không sửa ở đây (F01-P1).</summary>
public sealed record UpdateStudentCommand(string? FullName, RegistrationInput? Registration, string? Version);

public sealed class UpdateStudentHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, UpdateStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        if (command.Registration is null)
            return StudentErrors.RegistrationRequired;

        // @BR-f02-ho-so-hoc-vien-012: hồ sơ không tồn tại (hoặc của coach khác) → 404
        if (await students.GetForUpdateAsync(id, coach.Id, version, cancellationToken) is not { } student)
            return StudentErrors.NotFound;

        var registration = RegistrationForm.Create(command.Registration, clock.Today);
        student.UpdateRegistration(command.FullName, registration, coach.UserId, clock.UtcNow); // @BR-f02-ho-so-hoc-vien-013

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StudentErrors.VersionConflict; // @BR-f02-ho-so-hoc-vien-015 @ADR-f02-ho-so-hoc-vien-004
        }

        return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
