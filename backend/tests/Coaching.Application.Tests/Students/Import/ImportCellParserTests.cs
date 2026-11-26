using System.Globalization;
using Coaching.Application.Students.Import;

namespace Coaching.Application.Tests.Students.Import;

public sealed class ImportCellParserTests
{
    private static readonly WorkbookCell Blank = new(null, null, null);

    private static WorkbookCell Text(string text) => new(text, null, null);

    private static WorkbookCell Number(double number) => new(number.ToString(CultureInfo.InvariantCulture), number, null);

    private static WorkbookCell Date(DateTime date) =>
        new(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), null, date);

    [Fact]
    public void Blank_cell_is_accepted_as_empty_value()
    {
        Assert.True(ImportCellParser.TryInteger(Blank, out var integer, out _));
        Assert.Null(integer);
        Assert.True(ImportCellParser.TryDecimal(Blank, out var number, out _));
        Assert.Null(number);
        Assert.True(ImportCellParser.TryPrice(Blank, out var price, out _));
        Assert.Null(price);
        Assert.True(ImportCellParser.TryDate(Blank, out var date, out _));
        Assert.Null(date);
    }

    // @BR-f02-import-excel-031: ô ngày của Excel
    [Fact]
    public void Excel_date_cell_is_accepted()
    {
        Assert.True(ImportCellParser.TryDate(Date(new DateTime(2026, 11, 12, 9, 30, 0)), out var value, out var error));
        Assert.Equal(new DateOnly(2026, 11, 12), value);
        Assert.Null(error);
    }

    // @BR-f02-import-excel-031: chữ dd/MM/yyyy
    [Fact]
    public void Day_month_year_text_is_accepted()
    {
        Assert.True(ImportCellParser.TryDate(Text("25/11/2026"), out var value, out _));
        Assert.Equal(new DateOnly(2026, 11, 25), value);
    }

    // @BR-f02-import-excel-031
    [Theory]
    [InlineData("11/25/2026")]
    [InlineData("hôm qua")]
    [InlineData("2026-11-25")]
    [InlineData("25/11/26")]
    public void Other_date_text_is_invalid(string text)
    {
        Assert.False(ImportCellParser.TryDate(Text(text), out var value, out var error));
        Assert.Null(value);
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-031: ô số (không phải ô ngày) không phải ngày
    [Fact]
    public void Number_cell_is_not_a_date()
    {
        Assert.False(ImportCellParser.TryDate(Number(46345.91), out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-032: số với dấu chấm phân cách nghìn, có thể kèm "đ" / "VNĐ"
    [Theory]
    [InlineData("10.000.000đ")]
    [InlineData("10.000.000 VNĐ")]
    [InlineData("10.000.000")]
    [InlineData("10000000")]
    [InlineData("10000000 đ")]
    public void Price_text_is_accepted(string text)
    {
        Assert.True(ImportCellParser.TryPrice(Text(text), out var value, out var error));
        Assert.Equal(10_000_000L, value);
        Assert.Null(error);
    }

    // @BR-f02-import-excel-032: ô số nguyên
    [Fact]
    public void Whole_number_price_cell_is_accepted()
    {
        Assert.True(ImportCellParser.TryPrice(Number(10_000_000), out var value, out _));
        Assert.Equal(10_000_000L, value);
    }

    // @BR-f02-import-excel-032
    [Theory]
    [InlineData("10tr")]
    [InlineData("10000000,5")]
    [InlineData("-100")]
    [InlineData("10.00.000")]
    [InlineData("1.0000")]
    public void Other_price_text_is_invalid(string text)
    {
        Assert.False(ImportCellParser.TryPrice(Text(text), out var value, out var error));
        Assert.Null(value);
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-032
    [Fact]
    public void Fractional_price_cell_is_invalid()
    {
        Assert.False(ImportCellParser.TryPrice(Number(10_000_000.5), out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-023: ô số sai kiểu là ô không hợp lệ (tuổi là số nguyên)
    [Fact]
    public void Integer_reads_whole_number_cell_or_digits_and_rejects_others()
    {
        Assert.True(ImportCellParser.TryInteger(Number(21), out var fromCell, out _));
        Assert.Equal(21, fromCell);
        Assert.True(ImportCellParser.TryInteger(Text("21"), out var fromText, out _));
        Assert.Equal(21, fromText);
        Assert.False(ImportCellParser.TryInteger(Number(21.5), out _, out var fraction));
        Assert.False(string.IsNullOrEmpty(fraction));
        Assert.False(ImportCellParser.TryInteger(Text("hai mươi"), out _, out var words));
        Assert.False(string.IsNullOrEmpty(words));
    }

    // @BR-f02-import-excel-023: số đo nhận ô số hoặc chữ có dấu phẩy / chấm thập phân
    [Theory]
    [InlineData("72,5", "72.5")]
    [InlineData("72.5", "72.5")]
    [InlineData("170", "170")]
    public void Decimal_accepts_comma_or_dot(string text, string expected)
    {
        Assert.True(ImportCellParser.TryDecimal(Text(text), out var value, out _));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), value);
    }

    // @BR-f02-import-excel-023
    [Theory]
    [InlineData("170cm")]
    [InlineData("1,234.5")]
    public void Decimal_rejects_other_text(string text)
    {
        Assert.False(ImportCellParser.TryDecimal(Text(text), out var value, out var error));
        Assert.Null(value);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
