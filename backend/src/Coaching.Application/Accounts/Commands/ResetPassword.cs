using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts.Commands;

public sealed record ResetPasswordCommand(string? Token, string? Password, string? ConfirmPassword);

/// <summary>Đặt mật khẩu mới bằng link đặt lại (coach gửi hoặc tự xin).</summary>
public sealed class ResetPasswordHandler(
    IAccountTokens tokens,
    IAccountCredentials credentials,
    ISessionRevoker sessions,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-018 (now >= hạn), @BR-f01-p1-tai-khoan-hoc-vien-019 (đã dùng), @BR-f01-p1-tai-khoan-hoc-vien-020 (đã có link mới)
        if (await tokens.FindValidAsync(command.Token, AccountTokenPurpose.PasswordReset, cancellationToken) is not { } userId)
            return Result.Failure(AuthErrors.ResetLinkInvalid);

        // @BR-f01-p1-tai-khoan-hoc-vien-019: mật khẩu không hợp lệ → 400 và KHÔNG tiêu link (luật mật khẩu của kích hoạt áp nguyên)
        if (AuthErrors.ForPassword(PasswordPolicy.Check(command.Password, command.ConfirmPassword)) is { } invalid)
            return Result.Failure(invalid);

        var reset = await unitOfWork.ExecuteInTransactionAsync<Guid>(async ct =>
        {
            // @BR-f01-p1-tai-khoan-hoc-vien-019 @ADR-f01-p1-tai-khoan-hoc-vien-003: tiêu link TRƯỚC mọi thay đổi, có điều kiện trong
            // transaction — hai lần đặt lại đồng thời bằng cùng link, hoặc link bị thay sau bước kiểm tra ở trên → chỉ một lần thành công
            if (!await tokens.ConsumeAsync(command.Token!, ct))
                return Result<Guid>.Failure(AuthErrors.ResetLinkInvalid);

            await credentials.SetOwnPasswordAsync(userId, command.Password!, ct);
            await tokens.RevokeAllAsync(userId, AccountTokenPurpose.PasswordReset, ct);
            // @BR-f01-p1-tai-khoan-hoc-vien-021 @ADR-f01-p1-tai-khoan-hoc-vien-009: mọi phiên đang có kết thúc, chỉ mật khẩu mới đăng nhập được
            await sessions.RevokeAllAsync(userId, ct);
            return Result<Guid>.Success(userId);
        }, cancellationToken);
        return reset.IsSuccess ? Result.Success() : Result.Failure(reset.Error!);
    }
}
