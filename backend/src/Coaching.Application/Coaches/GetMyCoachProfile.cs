using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;

namespace Coaching.Application.Coaches;

public sealed record CoachProfileDto(Guid Id, string DisplayName, string Email);

/// <summary>Hồ sơ coach của admin đang đăng nhập.</summary>
public sealed class GetMyCoachProfileHandler(ICoachRepository coaches, ICurrentUser currentUser)
{
    public static readonly Error NotFound =
        new("coach.not_found", "Tài khoản này chưa có hồ sơ coach.", ErrorKind.NotFound);

    public async Task<Result<CoachProfileDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return new Error("auth.unauthenticated", "Chưa đăng nhập.", ErrorKind.Unauthorized);

        var coach = await coaches.GetByUserIdAsync(userId, cancellationToken);
        return coach is null
            ? NotFound
            : Result<CoachProfileDto>.Success(new CoachProfileDto(coach.Id, coach.DisplayName, coach.Email.Value));
    }
}
