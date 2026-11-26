using Coaching.Api.Common;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Danh mục nhóm cơ/dụng cụ của coach đang đăng nhập. MVP chỉ xem và thêm (spec §5).</summary>
// Route riêng (kebab-case) thay [controller] của AdminControllerBase; AdminOnly vẫn kế thừa (NFR-f03-thu-vien-bai-tap-002)
[Route("api/admin/exercise-categories")]
public sealed class ExerciseCategoriesController : AdminControllerBase
{
    // @BR-f03-thu-vien-bai-tap-005
    [HttpGet]
    [ProducesResponseType<CategoriesDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromServices] ListCategoriesHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-006
    [HttpPost]
    [ProducesResponseType<CategoryCreatedDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Add([FromBody] AddCategoryCommand command, [FromServices] AddCategoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.Error!.ToProblem(this);
    }
}
