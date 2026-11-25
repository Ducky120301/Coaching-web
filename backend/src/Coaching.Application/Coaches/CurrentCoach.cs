using Coaching.Application.Abstractions;
using Coaching.Domain.Coaches;

namespace Coaching.Application.Coaches;

/// <summary>Coach của admin đang đăng nhập. Mọi use case hồ sơ học viên lọc theo coach này.</summary>
public sealed class CurrentCoach(ICoachRepository coaches, ICurrentUser currentUser)
{
    public async Task<Coach?> GetAsync(CancellationToken cancellationToken = default) =>
        currentUser.UserId is { } userId ? await coaches.GetByUserIdAsync(userId, cancellationToken) : null;
}
