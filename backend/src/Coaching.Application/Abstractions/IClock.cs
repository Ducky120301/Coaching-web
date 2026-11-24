namespace Coaching.Application.Abstractions;

/// <summary>
/// Nguồn thời gian duy nhất. Không gọi DateTime.Now/UtcNow trong Domain/Application.
/// Ngày nghiệp vụ (tuần, hạn review, ngày check-in) luôn tính theo giờ Việt Nam (UTC+7).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Thời điểm hiện tại theo giờ Việt Nam.</summary>
    DateTimeOffset VietnamNow { get; }

    /// <summary>Ngày hôm nay theo giờ Việt Nam.</summary>
    DateOnly Today { get; }

    DateTimeOffset ToVietnamTime(DateTimeOffset instant);
}
