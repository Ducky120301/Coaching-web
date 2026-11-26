using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class YouTubeUrlTests
{
    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: mọi link http/https của youtube.com, *.youtube.com, youtu.be — lưu nguyên văn (đã trim)
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=vcBig73ojpE")]
    [InlineData("https://youtube.com/shorts/PfmfAeMpym4")]
    [InlineData("https://youtu.be/abc123XYZ09")]
    [InlineData("https://www.youtube.com/playlist?list=PL123abc")]
    [InlineData("https://www.youtube.com/@duobexiuuu")]
    [InlineData("http://m.youtube.com/watch?v=vcBig73ojpE")]
    [InlineData("HTTPS://WWW.YOUTUBE.COM/watch?v=vcBig73ojpE")]
    public void BR007_YouTube_links_are_kept_verbatim(string link) => Assert.Equal(link, YouTubeUrl.Normalize(link));

    [Fact]
    public void BR007_Link_is_trimmed() =>
        Assert.Equal("https://youtu.be/abc123XYZ09", YouTubeUrl.Normalize("  https://youtu.be/abc123XYZ09 "));

    // @BR-f03-thu-vien-bai-tap-007: không có link → lưu được (không video)
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BR007_No_link_means_no_video(string? link) => Assert.Null(YouTubeUrl.Normalize(link));

    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: không phải YouTube, host giả mạo, scheme lạ, không phải URL tuyệt đối
    [Theory]
    [InlineData("https://vimeo.com/123")]
    [InlineData("https://youtube.com.evil.com/watch?v=x")]
    [InlineData("https://notyoutube.com/watch?v=x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://youtube.com/video")]
    [InlineData("abc")]
    [InlineData("/watch?v=vcBig73ojpE")]
    public void BR007_Non_YouTube_links_are_rejected(string link) =>
        Assert.Equal("exercise.video_not_youtube", Assert.Throws<DomainException>(() => YouTubeUrl.Normalize(link)).Code);

    [Fact]
    public void BR007_Link_longer_than_2048_is_rejected() =>
        Assert.Equal("exercise.video_not_youtube", Assert.Throws<DomainException>(() =>
            YouTubeUrl.Normalize($"https://www.youtube.com/watch?v={new string('a', 2048)}")).Code);
}
