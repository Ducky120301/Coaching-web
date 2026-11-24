using Coaching.Application.Abstractions;

namespace Coaching.Infrastructure.Time;

/// <summary>
/// Việt Nam dùng UTC+7 cố định, không có giờ mùa hè → dùng offset cố định,
/// không phụ thuộc dữ liệu múi giờ của hệ điều hành/container.
/// </summary>
public sealed class VietnamClock(TimeProvider timeProvider) : IClock
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateTimeOffset VietnamNow => ToVietnamTime(UtcNow);

    public DateOnly Today => DateOnly.FromDateTime(VietnamNow.DateTime);

    public DateTimeOffset ToVietnamTime(DateTimeOffset instant) => instant.ToOffset(Offset);
}
