using System.Text;
using ClosedXML.Excel;

namespace Coaching.TestSupport;

/// <summary>
/// File .xlsx "câu trả lời Google Form" dựng trong bộ nhớ cho test import: dòng 1 = tiêu đề, dòng dữ liệu bắt đầu từ
/// dòng Excel 2. Đặt ô theo tiêu đề; tiêu đề chưa có thì thêm cột ở cuối.
/// </summary>
public sealed class ImportWorkbook
{
    public const string EmailHeader = "Địa chỉ email";
    public const string FullNameHeader = "Họ Và Tên (Name)";
    public const string AddressHeader = "Địa chỉ (Address)";
    public const string OccupationHeader = "Nghề nghiệp của bạn là gì?";
    public const string AgeHeader = "Tuổi (Age)";
    public const string TransferDateHeader = "Ngày chuyển khoản";
    public const string PriceHeader = "Giá gói bạn mua";
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string EncryptedResource = "Coaching.TestSupport.Fixtures.encrypted.xlsx";

    /// <summary>Chữ ký OLE compound file — dùng chung cho .xls (BIFF) và .xlsx đặt mật khẩu.</summary>
    private static readonly byte[] CompoundFileSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private readonly List<string> _headers;
    private readonly List<Dictionary<string, object?>> _rows = [];
    private readonly List<(string Email, string FullName)> _secondSheet = [];

    public ImportWorkbook(params string[] headers) => _headers = [.. headers];

    /// <summary>Email, họ tên + 2 cột chữ không bắt buộc (địa chỉ, nghề nghiệp).</summary>
    public static ImportWorkbook Standard() => new(EmailHeader, FullNameHeader, AddressHeader, OccupationHeader);

    public IReadOnlyList<string> Headers => _headers;

    /// <summary>Số dòng Excel của dòng dữ liệu cuối (tiêu đề = dòng 1).</summary>
    public int LastRow => _rows.Count + 1;

    /// <summary>Thêm dòng chỉ có email + họ tên (null = ô trống); trả số dòng Excel.</summary>
    public int AddRow(string? email, string? fullName)
    {
        _rows.Add(new Dictionary<string, object?> { [EmailHeader] = email, [FullNameHeader] = fullName });
        return LastRow;
    }

    /// <summary>Dòng hợp lệ: email, họ tên, và địa chỉ + nghề nghiệp nếu file có hai cột đó.</summary>
    public int AddValidRow(string email, string fullName)
    {
        var row = AddRow(email, fullName);
        Set(row, AddressHeader, "12 Lý Thường Kiệt, Hà Nội", addColumn: false);
        Set(row, OccupationHeader, "Kế toán", addColumn: false);
        return row;
    }

    /// <summary><paramref name="count"/> dòng hợp lệ, email <c>{prefix}{i}@gmail.com</c>, họ tên <c>Học viên {i}</c>.</summary>
    public ImportWorkbook AddValidRows(int count, string prefix = "hocvien")
    {
        for (var i = 1; i <= count; i++)
            AddValidRow($"{prefix}{i}@gmail.com", $"Học viên {i}");
        return this;
    }

    public int AddBlankRow()
    {
        _rows.Add(new Dictionary<string, object?>());
        return LastRow;
    }

    /// <summary>Đặt ô (dòng Excel, tiêu đề). Giá trị: string → ô chữ, DateTime/DateOnly → ô ngày, số → ô số, null → trống.</summary>
    public ImportWorkbook Set(int excelRow, string header, object? value, bool addColumn = true)
    {
        if (!_headers.Contains(header))
        {
            if (!addColumn)
                return this;
            _headers.Add(header);
        }
        _rows[excelRow - 2][header] = value;
        return this;
    }

    public object? Get(int excelRow, string header) => _rows[excelRow - 2].GetValueOrDefault(header);

    /// <summary>Bỏ mọi cột có tiêu đề bắt đầu bằng <paramref name="headerPrefix"/>.</summary>
    public ImportWorkbook WithoutColumn(string headerPrefix)
    {
        _headers.RemoveAll(h => h.StartsWith(headerPrefix, StringComparison.Ordinal));
        return this;
    }

    public ImportWorkbook MoveColumnBefore(string header, string before)
    {
        if (!_headers.Remove(header))
            throw new ArgumentException($"Không có cột \"{header}\".", nameof(header));
        var index = _headers.IndexOf(before);
        if (index < 0)
            throw new ArgumentException($"Không có cột \"{before}\".", nameof(before));
        _headers.Insert(index, header);
        return this;
    }

    /// <summary>Thêm một dòng (email, họ tên) ở sheet thứ hai (cùng tiêu đề email + họ tên).</summary>
    public ImportWorkbook AddSecondSheetRow(string email, string fullName)
    {
        _secondSheet.Add((email, fullName));
        return this;
    }

    public byte[] ToBytes()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Câu trả lời biểu mẫu 1");
        for (var c = 0; c < _headers.Count; c++)
            sheet.Cell(1, c + 1).Value = _headers[c];
        for (var r = 0; r < _rows.Count; r++)
        {
            for (var c = 0; c < _headers.Count; c++)
            {
                if (_rows[r].TryGetValue(_headers[c], out var value))
                    sheet.Cell(r + 2, c + 1).Value = ToCellValue(value);
            }
        }

        if (_secondSheet.Count > 0)
        {
            var second = workbook.AddWorksheet("Sheet2");
            second.Cell(1, 1).Value = EmailHeader;
            second.Cell(1, 2).Value = FullNameHeader;
            for (var i = 0; i < _secondSheet.Count; i++)
            {
                second.Cell(i + 2, 1).Value = _secondSheet[i].Email;
                second.Cell(i + 2, 2).Value = _secondSheet[i].FullName;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ---- File không phải .xlsx đọc được (@BR-f02-import-excel-014) ----

    public static byte[] Csv() => Encoding.UTF8.GetBytes("Địa chỉ email,Họ Và Tên (Name)\nlan@gmail.com,Nguyễn Thị Lan\n");

    /// <summary>Excel cũ .xls là OLE compound file (BIFF) — chữ ký D0 CF 11 E0 A1 B1 1A E1, không phải gói zip .xlsx.</summary>
    public static byte[] LegacyXls()
    {
        var bytes = new byte[4096];
        CompoundFileSignature.CopyTo(bytes, 0);
        return bytes;
    }

    /// <summary>.xlsx bị cắt nửa (zip hỏng).</summary>
    public static byte[] BrokenXlsx()
    {
        var bytes = Standard().AddValidRows(1).ToBytes();
        return bytes[..(bytes.Length / 2)];
    }

    /// <summary>.xlsx đặt mật khẩu thật (Fixtures/encrypted.xlsx, Excel lưu với mật khẩu — Fixtures/make-encrypted-xlsx.ps1).</summary>
    public static byte[] EncryptedXlsx()
    {
        using var resource = typeof(ImportWorkbook).Assembly.GetManifestResourceStream(EncryptedResource)
            ?? throw new InvalidOperationException("Thiếu Fixtures/encrypted.xlsx — chạy Fixtures/make-encrypted-xlsx.ps1.");
        using var copy = new MemoryStream();
        resource.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>File nặng đúng <paramref name="megabytes"/> MB: workbook thật ở đầu + phần đệm (bị từ chối theo kích thước trước khi đọc).</summary>
    public static byte[] OfSize(int megabytes)
    {
        var bytes = new byte[megabytes * 1024 * 1024];
        Standard().AddValidRows(1).ToBytes().CopyTo(bytes, 0);
        return bytes;
    }

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string text => text,
        DateTime date => date,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        int number => number,
        long number => number,
        double number => number,
        decimal number => (double)number,
        _ => throw new ArgumentException($"Kiểu ô không hỗ trợ: {value.GetType().Name}", nameof(value)),
    };
}
