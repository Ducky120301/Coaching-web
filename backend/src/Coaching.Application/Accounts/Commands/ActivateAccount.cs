using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts.Commands;

public sealed record ActivateAccountCommand(string? ActivationToken, string? Password, string? ConfirmPassword);

/// <summary>Học viên (đã đăng nhập bằng mật khẩu tạm còn hạn) đặt mật khẩu riêng → Đã kích hoạt → vào thẳng.</summary>
public sealed class ActivateAccountHandler(
    IAccountTokens tokens,
    IStudentRepository students,
    IAccountCredentials credentials,
    IAuthService auth,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<AuthTokens>> HandleAsync(ActivateAccountCommand command, CancellationToken cancellationToken = default)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-011: quyền đặt mật khẩu chỉ đến từ mã kích hoạt còn hiệu lực (không có JWT)
        if (await tokens.FindValidAsync(command.ActivationToken, AccountTokenPurpose.Activation, cancellationToken) is not { } userId)
            return AuthErrors.ActivationInvalid;

        // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: lỗi mật khẩu trả 400 và KHÔNG tiêu mã kích hoạt
        if (AuthErrors.ForPassword(PasswordPolicy.Check(command.Password, command.ConfirmPassword)) is { } invalid)
            return invalid;

        var activated = await unitOfWork.ExecuteInTransactionAsync<Guid>(async ct =>
        {
            var now = clock.UtcNow;
            if (await students.GetTrackedByUserIdAsync(userId, ct) is not { } student || !student.IsTempPasswordValid(now))
                return Result<Guid>.Failure(AuthErrors.ActivationInvalid);

            student.Activate(now); // @BR-f01-p1-tai-khoan-hoc-vien-016
            await unitOfWork.SaveChangesAsync(ct);
            // @BR-f01-p1-tai-khoan-hoc-vien-015: lưu đúng từng ký tự; @BR-f01-p1-tai-khoan-hoc-vien-016: mật khẩu tạm mất hiệu lực ngay
            await credentials.SetOwnPasswordAsync(userId, command.Password!, ct);
            await tokens.ConsumeAsync(command.ActivationToken!, ct);
            return Result<Guid>.Success(userId);
        }, cancellationToken);
        if (!activated.IsSuccess)
            return activated.Error!;

        return await auth.IssueSessionAsync(userId, cancellationToken); // @BR-f01-p1-tai-khoan-hoc-vien-016: vào thẳng
    }
}
