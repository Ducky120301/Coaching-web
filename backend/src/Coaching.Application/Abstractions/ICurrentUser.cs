namespace Coaching.Application.Abstractions;

/// <summary>Người dùng của request hiện tại (lấy từ JWT).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    string? FullName { get; }

    bool IsInRole(string role);
}
