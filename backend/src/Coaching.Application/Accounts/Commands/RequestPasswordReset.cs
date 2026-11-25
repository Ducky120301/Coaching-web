using Coaching.Application.Abstractions;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts.Commands;

public sealed record ForgotPasswordCommand(string? Email);

/// <summary>@BR-f01-p1-tai-khoan-hoc-vien-023: kết quả duy nhất của Quên mật khẩu, mọi trường hợp như nhau.</summary>
public sealed record ForgotPasswordResponse(string Message)
{
    public const string GenericMessage =
        "Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.";

    public static ForgotPasswordResponse Generic { get; } = new(GenericMessage);
}

/// <summary>Học viên tự xin link đặt lại qua Quên mật khẩu. Không ghi lịch sử tài khoản (chỉ có vết trong AccountTokens).</summary>
public sealed class RequestPasswordResetHandler(
    IAccountCredentials credentials,
    IStudentRepository students,
    IAccountTokens tokens,
    IAccountEmails emails,
    IEmailQueue queue,
    IClock clock)
{
    public async Task<ForgotPasswordResponse> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        await TryIssueAsync(command.Email, cancellationToken);
        return ForgotPasswordResponse.Generic; // @BR-f01-p1-tai-khoan-hoc-vien-023: không lộ email nào có tài khoản
    }

    private async Task TryIssueAsync(string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;
        // @BR-f01-p1-tai-khoan-hoc-vien-022: chỉ học viên Đã kích hoạt; email bỏ khoảng trắng đầu/cuối, không phân biệt hoa/thường.
        // Email lạ, học viên Chờ kích hoạt, tài khoản coach (không có Student) → không gửi gì
        if (await credentials.FindUserIdByEmailAsync(email.Trim(), ct) is not { } userId)
            return;
        if (await students.GetByUserIdAsync(userId, ct) is not { AccountStatus: AccountStatus.Activated } student)
            return;

        var now = clock.UtcNow;
        // @BR-f01-p1-tai-khoan-hoc-vien-024 @ADR-f01-p1-tai-khoan-hoc-vien-005: theo tài khoản, dựa trên link tự xin gần nhất
        if (await tokens.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, ct) is { } last
            && now - last < AccountTokenLifetimes.SelfResetInterval)
            return;

        // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-020: link 30 phút, hủy mọi link cũ (kể cả link coach gửi)
        var rawToken = await tokens.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self,
            now + AccountTokenLifetimes.PasswordReset, ct);
        var message = emails.PasswordReset(student.Email.Value, student.FullName, rawToken);
        if (message.IsSuccess)
            await queue.EnqueueAsync(message.Value, ct); // @ADR-f01-p1-tai-khoan-hoc-vien-005: gửi nền, phản hồi không chờ dịch vụ email
    }
}
