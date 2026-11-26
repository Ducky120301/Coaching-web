using Coaching.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Common;

internal static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : result.Error!.ToProblem(controller);

    public static IActionResult ToProblem(this Error error, ControllerBase controller)
    {
        var status = error.Kind switch
        {
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };

        var problem = new ProblemDetails { Status = status, Title = error.Message, Type = error.Code };
        problem.Extensions["code"] = error.Code;
        // vd. hiddenExerciseId của exercise.duplicate (BR-f03-thu-vien-bai-tap-001)
        if (error.Extensions is not null)
        {
            foreach (var (key, value) in error.Extensions)
                problem.Extensions[key] = value;
        }
        return new ObjectResult(problem) { StatusCode = status };
    }
}
