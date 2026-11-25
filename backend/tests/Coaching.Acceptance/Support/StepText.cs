using System.Globalization;
using System.Text.RegularExpressions;

namespace Coaching.Acceptance.Support;

public static class StepText
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    /// <summary>Các chuỗi trong ngoặc kép theo thứ tự: <c>"a", "b" và "c"</c> → [a, b, c].</summary>
    public static IReadOnlyList<string> Quoted(string text) =>
        Regex.Matches(text, "\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

    public static DateOnly ParseDate(string ddMMyyyy) =>
        DateOnly.ParseExact(ddMMyyyy, "dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static DateTimeOffset VietnamTime(string ddMMyyyy, int hour, int minute)
    {
        var date = ParseDate(ddMMyyyy);
        return new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, VietnamOffset);
    }

    public static DateOnly VietnamDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(VietnamOffset).DateTime);
}
