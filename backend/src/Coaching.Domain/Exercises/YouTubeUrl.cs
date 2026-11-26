using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-005 @BR-f03-thu-vien-bai-tap-007: link video mẫu là URI tuyệt đối http/https, host youtube.com,
/// youtu.be hoặc kết thúc bằng ".youtube.com"; lưu nguyên văn (đã trim). Không kiểm video có tồn tại (spec §5).
/// FE chỉ nhúng khi rút được mã video 11 ký tự (youtube-nocookie.com/embed/&lt;id&gt;).
/// </summary>
public static class YouTubeUrl
{
    public const int MaxLength = Exercise.VideoUrlMaxLength;

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var url = raw.Trim();
        if (url.Length <= MaxLength
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && IsYouTubeHost(uri.Host))
            return url;

        throw new DomainException("exercise.video_not_youtube", "Link video phải là link YouTube (youtube.com hoặc youtu.be).");
    }

    private static bool IsYouTubeHost(string host)
    {
        var h = host.ToLowerInvariant();
        return h is "youtube.com" or "youtu.be" || h.EndsWith(".youtube.com", StringComparison.Ordinal);
    }
}
