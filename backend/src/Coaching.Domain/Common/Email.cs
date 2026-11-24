using System.Net.Mail;

namespace Coaching.Domain.Common;

/// <summary>Địa chỉ email đã chuẩn hóa (trim + lowercase).</summary>
public sealed record Email
{
    public const int MaxLength = 256;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
            throw new DomainException("email.required", "Email là bắt buộc.");
        if (value.Length > MaxLength
            || !MailAddress.TryCreate(value, out var parsed)
            || parsed.Address != value
            || !parsed.Host.Contains('.'))
            throw new DomainException("email.invalid", "Email không hợp lệ.");
        return new Email(value);
    }

    public override string ToString() => Value;
}
