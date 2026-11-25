using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <param name="Status">"Active" | "Paused" | "Ended".</param>
public sealed record ChangeCoachingStatusCommand(string? Status, string? Reason, string? Version);

public sealed class ChangeCoachingStatusHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IAuditLog audit,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, ChangeCoachingStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Status is null || !Enum.GetNames<CoachingStatus>().Contains(command.Status))
            return StudentErrors.StatusInvalid;
        var status = Enum.Parse<CoachingStatus>(command.Status);

        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        if (await students.GetForUpdateAsync(id, coach.Id, version, cancellationToken) is not { } student)
            return StudentErrors.NotFound;

        var from = student.CoachingStatus;
        // @BR-f02-ho-so-hoc-vien-017..022 @ADR-f02-ho-so-hoc-vien-005: invariant nằm trong aggregate
        student.ChangeCoachingStatus(status, command.Reason, coach.UserId, clock.UtcNow);
        // NFR-f02-ho-so-hoc-vien-002: audit chỉ ghi From→To (người + thời điểm do IAuditLog lấy) — không lý do, không ghi chú
        audit.Record("coaching_status.changed", "Student", student.Id.ToString(), $"{from}→{status}");

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StudentErrors.VersionConflict; // @BR-f02-ho-so-hoc-vien-015: hai lệnh cùng phiên bản → chỉ một thành công
        }

        return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
