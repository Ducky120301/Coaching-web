using Coaching.Api.Common;
using Coaching.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.IntegrationTests.Common;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void Unavailable_error_maps_to_503_problem_with_code()
    {
        var error = new Error("account.email_send_failed", "Chưa gửi được email. Vui lòng thử lại sau.", ErrorKind.Unavailable);

        var result = Assert.IsType<ObjectResult>(error.ToProblem(new StubController()));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.Status);
        Assert.Equal("account.email_send_failed", problem.Extensions["code"]);
        Assert.Equal("Chưa gửi được email. Vui lòng thử lại sau.", problem.Title);
    }

    private sealed class StubController : ControllerBase;
}
