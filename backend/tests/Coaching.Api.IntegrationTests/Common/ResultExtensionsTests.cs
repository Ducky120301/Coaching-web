using Coaching.Api.Common;
using Coaching.Application.Common;
using Coaching.Application.Exercises;
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

    // @BR-f03-thu-vien-bai-tap-001: bài trùng đang ẩn → ProblemDetails mang hiddenExerciseId để gợi ý bỏ ẩn
    [Fact]
    public void Error_extensions_are_copied_to_problem()
    {
        var hiddenId = Guid.NewGuid();

        var result = Assert.IsType<ObjectResult>(ExerciseErrors.Duplicate(hiddenId).ToProblem(new StubController()));

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("exercise.duplicate", problem.Extensions["code"]);
        Assert.Equal("Đã có bài cùng tên và dụng cụ.", problem.Title);
        Assert.Equal(hiddenId, problem.Extensions["hiddenExerciseId"]);
        Assert.False(Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(
            ExerciseErrors.Duplicate(null).ToProblem(new StubController())).Value).Extensions.ContainsKey("hiddenExerciseId"));
    }


    private sealed class StubController : ControllerBase;
}
