using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

public sealed record UpdateInternalNoteCommand(string? Note, string? Version);

/// <summary>Ghi chú nội bộ không ghi audit (NFR-f02-ho-so-hoc-vien-002: audit không lưu nội dung ghi chú).</summary>
public sealed class UpdateInternalNoteHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, UpdateInternalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        if (await students.GetForUpdateAsync(id, coach.Id, version, cancellationToken) is not { } student)
            return StudentErrors.NotFound;

        student.UpdateInternalNote(command.Note, clock.UtcNow); // @BR-f02-ho-so-hoc-vien-014

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StudentErrors.VersionConflict;
        }

        return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, clock.UtcNow));
    }
}
