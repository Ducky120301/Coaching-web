using System.Text;
using Coaching.Domain.Students;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-002: một định nghĩa "trùng" cho bài tập và danh mục — SearchText.Normalize (bỏ dấu, đ → d,
/// lowercase, trim) + đồng nhất mọi kiểu dấu gạch (‐ ‑ ‒ – — ― −) thành "-" + gộp khoảng trắng liên tiếp thành một.
/// Dùng cho Exercise.NameKey/SearchKey và ExerciseCategory.NameKey.
/// </summary>
public static class ExerciseNameKey
{
    public static string For(string? name)
    {
        var normalized = SearchText.Normalize(name);
        var builder = new StringBuilder(normalized.Length);
        var pendingSpace = false;
        foreach (var c in normalized)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }
            builder.Append(IsDash(c) ? '-' : c);
        }
        return builder.ToString();
    }

    private static bool IsDash(char c) => c is '‐' or '‑' or '‒' or '–' or '—' or '―' or '−';
}
