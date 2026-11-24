using Coaching.Infrastructure.Time;

namespace Coaching.Api.IntegrationTests.Infrastructure;

public sealed class VietnamClockTests
{
    [Fact]
    public void Today_uses_vietnam_date_not_utc_date()
    {
        // 23/11/2026 17:30 UTC = 24/11/2026 00:30 giờ Việt Nam
        var clock = new VietnamClock(new FixedTimeProvider(new DateTimeOffset(2026, 11, 23, 17, 30, 0, TimeSpan.Zero)));

        Assert.Equal(new DateOnly(2026, 11, 24), clock.Today);
        Assert.Equal(TimeSpan.FromHours(7), clock.VietnamNow.Offset);
        Assert.Equal(0, clock.VietnamNow.Hour);
    }

    [Fact]
    public void Today_before_vietnam_midnight_is_previous_day()
    {
        // 23/11/2026 16:59 UTC = 23/11/2026 23:59 giờ Việt Nam
        var clock = new VietnamClock(new FixedTimeProvider(new DateTimeOffset(2026, 11, 23, 16, 59, 0, TimeSpan.Zero)));

        Assert.Equal(new DateOnly(2026, 11, 23), clock.Today);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
