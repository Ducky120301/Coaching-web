namespace Coaching.Domain.Students;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-006: 5 loại dòng lịch sử tài khoản.</summary>
public enum AccountEventType
{
    CredentialsSent,
    CredentialsResent,
    EmailChanged,
    ResetLinkSent,
    Activated,
}
