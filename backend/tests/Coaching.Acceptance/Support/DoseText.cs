using System.Globalization;

namespace Coaching.Acceptance.Support;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-006: chữ liều như coach gõ ("10", "10–12", "10-12") ↔ số có cấu trúc của API;
/// hiển thị lại "10–12" (dấu gạch ngang en), "10" khi thấp = cao, "90s", trống là "—" (FE làm cùng quy tắc).
/// </summary>
public static class DoseText
{
    public static (int Min, int Max) ParseRange(string text)
    {
        var parts = text.Split(new[] { '–', '-' }, StringSplitOptions.TrimEntries);
        var min = int.Parse(parts[0], CultureInfo.InvariantCulture);
        return (min, parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : min);
    }

    public static string FormatRange(int? min, int? max) =>
        min is null ? "—" : min == max ? $"{min}" : $"{min}–{max}";

    public static string FormatRest(int? seconds) => seconds is null ? "—" : $"{seconds}s";
}
