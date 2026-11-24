namespace Coaching.Domain.Common;

/// <summary>Vi phạm business rule. <see cref="Code"/> nên gắn mã BR-xx khi có.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
