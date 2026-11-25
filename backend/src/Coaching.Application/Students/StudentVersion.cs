namespace Coaching.Application.Students;

/// <summary>@ADR-f02-ho-so-hoc-vien-004: version trong API là base64 của rowversion.</summary>
internal static class StudentVersion
{
    public static string Format(byte[] version) => Convert.ToBase64String(version);

    public static byte[]? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) && written > 0 ? buffer[..written] : null;
    }
}
