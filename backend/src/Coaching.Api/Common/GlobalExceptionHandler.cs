using Coaching.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Common;

/// <summary>DomainException → 400 kèm mã lỗi; lỗi khác → 500 không lộ stack trace.</summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        if (exception is DomainException domain)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = domain.Message,
                Type = domain.Code,
            };
            problem.Extensions["code"] = domain.Code;
        }
        else
        {
            logger.LogError(exception, "Lỗi không xử lý được khi gọi {Path}", httpContext.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Đã có lỗi xảy ra. Vui lòng thử lại sau.",
            };
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
