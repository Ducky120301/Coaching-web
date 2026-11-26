using System.Globalization;
using System.Text.RegularExpressions;

namespace Coaching.Application.Students.Import;

/// <summary>
/// Đọc ô số / ngày / giá của file import. Ô trống → true, value null (mục để trống). Sai dạng → false + lý do tiếng Việt
/// (@BR-f02-import-excel-023: ô sai làm bỏ cả dòng). Khoảng giá trị (tuổi, số đo, giá ≥ 0, ngày ≤ hôm nay) do
/// RegistrationForm.Validate kiểm.
/// </summary>
public static partial class ImportCellParser
{
    private const string NotInteger = "phải là số nguyên";
    private const string NotNumber = "phải là số";
    private const string NotPrice = "phải là số nguyên, có thể có dấu chấm phân cách nghìn và đuôi \"đ\" hoặc \"VNĐ\"";
    private const string NotDate = "phải là ngày dạng ngày/tháng/năm (dd/MM/yyyy)";

    /// <summary>Ngoài khoảng này double không còn chính xác tới hàng đơn vị.</summary>
    private const double MaxWholeNumber = 1e15;

    public static bool TryInteger(WorkbookCell c, out int? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Number is { } number)
        {
            if (number != Math.Floor(number) || number < int.MinValue || number > int.MaxValue)
                return Fail(NotInteger, out error);
            value = (int)number;
            return true;
        }
        if (c.Date is not null)
            return Fail(NotInteger, out error);
        if (c.Text is null)
            return true;
        if (!int.TryParse(c.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
            return Fail(NotInteger, out error);
        value = parsed;
        return true;
    }

    /// <summary>Ô số, hoặc chữ số thập phân với dấu phẩy hoặc dấu chấm ("72,5" = "72.5").</summary>
    public static bool TryDecimal(WorkbookCell c, out decimal? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Number is { } number)
        {
            if (Math.Abs(number) > MaxWholeNumber)
                return Fail(NotNumber, out error);
            value = (decimal)number;
            return true;
        }
        if (c.Date is not null)
            return Fail(NotNumber, out error);
        if (c.Text is null)
            return true;
        if (!decimal.TryParse(c.Text.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var parsed))
            return Fail(NotNumber, out error);
        value = parsed;
        return true;
    }

    /// <summary>@BR-f02-import-excel-032: ô số nguyên, hoặc chữ gồm số với dấu chấm phân cách nghìn, có thể kèm "đ" / "VNĐ".</summary>
    public static bool TryPrice(WorkbookCell c, out long? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Number is { } number)
        {
            if (number != Math.Floor(number) || Math.Abs(number) > MaxWholeNumber)
                return Fail(NotPrice, out error);
            value = (long)number;
            return true;
        }
        if (c.Date is not null)
            return Fail(NotPrice, out error);
        if (c.Text is null)
            return true;
        var match = PriceText().Match(c.Text);
        if (!match.Success
            || !long.TryParse(match.Groups["digits"].Value.Replace(".", string.Empty, StringComparison.Ordinal),
                NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            return Fail(NotPrice, out error);
        value = parsed;
        return true;
    }

    /// <summary>@BR-f02-import-excel-031: ô ngày của Excel, hoặc chữ dd/MM/yyyy.</summary>
    public static bool TryDate(WorkbookCell c, out DateOnly? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Date is { } date)
        {
            value = DateOnly.FromDateTime(date);
            return true;
        }
        if (c.Number is not null)
            return Fail(NotDate, out error);
        if (c.Text is null)
            return true;
        if (!DateOnly.TryParseExact(c.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return Fail(NotDate, out error);
        value = parsed;
        return true;
    }

    private static bool Fail(string reason, out string? error)
    {
        error = reason;
        return false;
    }

    [GeneratedRegex(@"^(?<digits>[0-9]{1,3}(?:\.[0-9]{3})+|[0-9]+)\s*(?:đ|vnđ)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PriceText();
}
