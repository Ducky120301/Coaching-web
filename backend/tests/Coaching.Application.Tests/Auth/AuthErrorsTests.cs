using Coaching.Application.Auth;
using Coaching.Application.Common;

namespace Coaching.Application.Tests.Auth;

public sealed class AuthErrorsTests
{
    // @BR-f00-codebase-004 @BR-f00-codebase-005
    [Theory]
    [InlineData(ClientApp.Admin, "Tài khoản này không có quyền vào khu vực coach.")]
    [InlineData(ClientApp.Client, "Tài khoản này không có quyền vào khu vực học viên.")]
    public void WrongApp_message_per_app(ClientApp app, string message)
    {
        var error = AuthErrors.WrongApp(app);
        Assert.Equal("auth.wrong_app", error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(ErrorKind.Forbidden, error.Kind);
    }
}
