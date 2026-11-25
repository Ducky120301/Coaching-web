using System.Globalization;
using System.Text;

namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-003: chuẩn hóa để tìm không dấu. Collation *_CI_AI của SQL Server không coi "đ" = "d",
/// nên tự bỏ dấu ở Domain: đ/Đ → d, tách dấu (FormD) rồi bỏ dấu kết hợp, lowercase.
/// </summary>
public static class SearchText
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var decomposed = text.Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
