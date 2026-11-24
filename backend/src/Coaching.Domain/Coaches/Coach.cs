using Coaching.Domain.Common;

namespace Coaching.Domain.Coaches;

/// <summary>
/// Coach phụ trách học viên. MVP chỉ có 1 coach (admin được seed),
/// nhưng học viên luôn gắn CoachId để sau này mở rộng nhiều coach.
/// </summary>
public sealed class Coach : AggregateRoot<Guid>
{
    public const int DisplayNameMaxLength = 100;

    /// <summary>Id tài khoản đăng nhập (Identity user) của coach.</summary>
    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; } = default!;

    public Email Email { get; private set; } = default!;

    private Coach() { }

    private Coach(Guid id) : base(id) { }

    public static Coach Create(Guid userId, string displayName, Email email)
    {
        if (userId == Guid.Empty)
            throw new DomainException("coach.user_required", "Coach phải gắn với một tài khoản đăng nhập.");

        return new Coach(Guid.CreateVersion7())
        {
            UserId = userId,
            DisplayName = NormalizeName(displayName),
            Email = email,
        };
    }

    public void Rename(string displayName) => DisplayName = NormalizeName(displayName);

    private static string NormalizeName(string? displayName)
    {
        var name = displayName?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new DomainException("coach.name_required", "Tên coach là bắt buộc.");
        if (name.Length > DisplayNameMaxLength)
            throw new DomainException("coach.name_too_long", $"Tên coach tối đa {DisplayNameMaxLength} ký tự.");
        return name;
    }
}
